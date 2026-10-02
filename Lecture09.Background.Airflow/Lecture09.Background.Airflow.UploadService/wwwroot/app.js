const videosElement = document.getElementById("videos");
const titleElement = document.getElementById("title");
const template = document.getElementById("video-template");

function renderVideo(video) {
    const card = template.content.firstElementChild.cloneNode(true);
    card.querySelector("[data-title]").textContent = video.title;
    card.querySelector("[data-id]").textContent = video.videoId;

    const link = card.querySelector("[data-run]");
    link.href = video.dagRunUrl;
    link.textContent = `запуск ${video.dagRunId}`;

    videosElement.prepend(card);
}

async function post(url, body) {
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body ?? {})
    });
    return response.json();
}

document.getElementById("upload").onclick = async () => {
    renderVideo(await post("/videos", { title: titleElement.value }));
    titleElement.value = "";
};

document.getElementById("upload-batch").onclick = async () => {
    (await post("/videos/batch", { count: 10 })).forEach(renderVideo);
};
