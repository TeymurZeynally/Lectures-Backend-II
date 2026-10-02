import random
import socket
import time
from datetime import timedelta

import pendulum
from airflow.sdk import dag, task


# @dag превращает функцию ниже в описание графа, @task - функцию в вершину графа.
# Тело задачи выполняется не здесь, а позже, когда Scheduler запустит эту вершину в своём процессе
@dag(
    dag_id="video_processing",
    # Когда запускать граф:
    #   schedule=None                        - только по триггеру: кнопка Trigger в UI, REST API, CLI (как сейчас)
    #   schedule="0 3 * * *"                 - по cron: каждый день в 03:00
    #   schedule="@hourly"                   - готовые варианты: @hourly, @daily, @weekly, @monthly
    #   schedule=timedelta(minutes=30)       - с фиксированным интервалом
    #   schedule=[Asset("videos/uploaded")]  - по событию: когда другой граф обновил этот Asset
    schedule=None,
    # С какой даты считать запуски по расписанию и нужно ли догонять пропущенные. При schedule=None не важно
    start_date=pendulum.datetime(2026, 1, 1, tz="UTC"),
    catchup=False,
    # Значения по умолчанию: подставляются в форму Trigger, а conf из REST API их перекрывает
    params={"video_id": "3f2a9c1e", "title": "Путешествие на Байкал"},
    # Каждая упавшая задача повторяется до 3 раз через 5 секунд, остальные вершины не трогаются
    default_args={"retries": 3, "retry_delay": timedelta(seconds=5)},
    tags=["lecture09"],
)
def video_processing():
    @task
    def validate_format(params=None):
        print(f"[{socket.gethostname()}] Validating format of video {params['video_id']} \"{params['title']}\"")
        time.sleep(2)
        return params["video_id"]

    @task
    def extract_audio(video_id):
        print(f"[{socket.gethostname()}] Extracting audio track of video {video_id}")
        time.sleep(3)
        return f"{video_id}/audio.aac"

    @task
    def recognize_speech(audio):
        print(f"[{socket.gethostname()}] Recognizing speech in {audio}")
        time.sleep(5)
        return f"{audio.split('/')[0]}/transcript.txt"

    @task
    def create_subtitles(transcript):
        print(f"[{socket.gethostname()}] Creating subtitles from {transcript}")
        time.sleep(2)
        return f"{transcript.split('/')[0]}/subtitles.vtt"

    @task
    def transcode(video_id, quality):
        print(f"[{socket.gethostname()}] Transcoding video {video_id} to {quality}")
        time.sleep(6)

        if random.random() < 0.3:
            raise RuntimeError(f"Transcoder crashed on {quality}")

        return f"{video_id}/{quality}.mp4"

    @task
    def package_hls(video_id, renditions):
        print(f"[{socket.gethostname()}] Packaging HLS playlist of video {video_id} from {list(renditions)}")
        time.sleep(2)
        return f"{video_id}/master.m3u8"

    @task
    def extract_cover_frames(video_id):
        print(f"[{socket.gethostname()}] Extracting cover frames of video {video_id}")
        time.sleep(3)
        return [f"{video_id}/frame-{i}.jpg" for i in range(1, 6)]

    @task
    def choose_cover(frames):
        cover = random.choice(frames)
        print(f"[{socket.gethostname()}] Choosing cover among {len(frames)} frames: {cover}")
        time.sleep(1)
        return cover

    @task
    def publish(video_id, subtitles, playlist, cover):
        print(f"[{socket.gethostname()}] Publishing video {video_id}: {playlist}, {subtitles}, {cover}")
        time.sleep(1)
        return video_id

    @task
    def notify_subscribers(video_id):
        print(f"[{socket.gethostname()}] Notifying subscribers about video {video_id}")
        time.sleep(1)

    # Здесь только связи между вершинами: какой результат какой задаче передаётся
    video_id = validate_format()

    subtitles = create_subtitles(recognize_speech(extract_audio(video_id)))
    playlist = package_hls(video_id, transcode.partial(video_id=video_id).expand(quality=["1080p", "720p", "480p"]))
    cover = choose_cover(extract_cover_frames(video_id))

    notify_subscribers(publish(video_id, subtitles, playlist, cover))


# Строит граф. Этот файл раз в 10 секунд выполняет DAG Processor, обработка ролика при этом не запускается
video_processing()
