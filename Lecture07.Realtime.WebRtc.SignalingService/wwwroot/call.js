import { connect, showStatus } from "./rtc.js";

const room = document.getElementById("room");
const mode = document.getElementById("mode");
const source = document.getElementById("source");
const joinButton = document.getElementById("join");
const status = document.getElementById("status");
const localVideo = document.getElementById("local");
const remoteVideo = document.getElementById("remote");

joinButton.onclick = async () => {
    joinButton.disabled = true;

    // Получаем медиапоток (MediaStream) - набор треков, аудио и видео:
    const localStream = source.value === "screen"
        ? await navigator.mediaDevices.getDisplayMedia({ video: true, audio: true })
        : await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

    // Показываем себе своё же видео (у элемента стоит muted, чтобы не было эха)
    localVideo.srcObject = localStream;

    const peerConnection = await connect(room.value, mode.value, peerConnection => {
        // Добавляем каждый трек в соединение ДО обмена offer/answer - так они попадут в SDP
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

        // Собеседник прислал свой трек - показываем его поток во втором видео
        peerConnection.ontrack = event => remoteVideo.srcObject = event.streams[0];
    });

    showStatus(peerConnection, status);
};
