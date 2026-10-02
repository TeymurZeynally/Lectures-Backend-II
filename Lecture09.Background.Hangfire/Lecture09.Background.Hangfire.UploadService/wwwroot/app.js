const videosElement = document.getElementById("videos");
const titleElement = document.getElementById("title");
const template = document.getElementById("video-template");

function renderVideo(video) {
    const card = template.content.firstElementChild.cloneNode(true);
    card.querySelector("[data-title]").textContent = video.title;
    card.querySelector("[data-id]").textContent = video.videoId;

    for (const job of video.jobs) {
        const link = document.createElement("a");
        link.href = `/hangfire/jobs/details/${job.jobId}`;
        link.target = "_blank";
        link.className = "px-2 py-1 rounded-md bg-slate-100 text-slate-700 text-xs hover:bg-slate-200";
        link.textContent = `${job.name} #${job.jobId}`;
        card.querySelector("[data-jobs]").append(link);
    }

    videosElement.prepend(card);
}

async function post(url, body) {
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body ?? {})
    });
    return response.headers.get("Content-Type")?.includes("json") ? response.json() : null;
}

document.getElementById("upload").onclick = async () => {
    renderVideo(await post("/videos", { title: titleElement.value }));
    titleElement.value = "";
};

document.getElementById("upload-batch").onclick = async () => {
    (await post("/videos/batch", { count: 10 })).forEach(renderVideo);
};

document.getElementById("recalculate").onclick = () => post("/statistics/recalculate");

async function refreshSummary() {
    const summary = await (await fetch("/jobs/summary")).json();
    for (const [name, value] of Object.entries(summary)) {
        const element = document.querySelector(`[data-stat="${name}"]`);
        if (element) element.textContent = value;
    }
}

refreshSummary();
setInterval(refreshSummary, 1000);
