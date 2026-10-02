const videosElement = document.getElementById("videos");
const titleElement = document.getElementById("title");
const template = document.getElementById("video-template");

async function post(url, body) {
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body ?? {})
    });
    return response.headers.get("Content-Type")?.includes("json") ? response.json() : null;
}

function renderVideo(video) {
    const card = template.content.firstElementChild.cloneNode(true);
    card.querySelector("[data-title]").textContent = video.title;
    card.querySelector("[data-id]").textContent = video.videoId;

    const link = card.querySelector("[data-workflow]");
    link.href = video.workflowUrl;
    link.textContent = `workflow ${video.workflowId}`;

    card.querySelector("[data-approve]").onclick = () => post(`/videos/${video.videoId}/approve`);
    card.querySelector("[data-reject]").onclick = () =>
        post(`/videos/${video.videoId}/reject`, { reason: card.querySelector("[data-reason]").value });

    videosElement.prepend(card);
    watch(video.videoId, card);
}

async function watch(videoId, card) {
    const status = await (await fetch(`/videos/${videoId}`)).json();

    card.querySelector("[data-status]").textContent = status.status;
    const approval = card.querySelector("[data-approval]");
    approval.classList.toggle("hidden", !status.waitingForApproval);
    approval.classList.toggle("flex", status.waitingForApproval);

    if (!status.finished) {
        setTimeout(() => watch(videoId, card), 1500);
    }
}

document.getElementById("upload").onclick = async () => {
    renderVideo(await post("/videos", { title: titleElement.value }));
    titleElement.value = "";
};

document.getElementById("upload-batch").onclick = async () => {
    (await post("/videos/batch", { count: 10 })).forEach(renderVideo);
};
