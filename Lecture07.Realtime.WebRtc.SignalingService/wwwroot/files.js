import { connect, showStatus } from "./rtc.js";

const CHUNK_SIZE = 64 * 1024;
const MEGABYTE = 1024 * 1024;

const room = document.getElementById("room");
const mode = document.getElementById("mode");
const joinButton = document.getElementById("join");
const status = document.getElementById("status");
const fileInput = document.getElementById("file");
const sendButton = document.getElementById("send");
const progressBar = document.getElementById("progress-bar");
const progressText = document.getElementById("progress-text");
const receivedList = document.getElementById("received");

let channel;
let incoming;

joinButton.onclick = async () => {
    joinButton.disabled = true;

    const peerConnection = await connect(room.value, mode.value, peerConnection => {
        channel = peerConnection.createDataChannel("files", { negotiated: true, id: 0 });
    });

    showStatus(peerConnection, status);

    channel.binaryType = "arraybuffer";
    channel.bufferedAmountLowThreshold = MEGABYTE;
    channel.onopen = () => sendButton.disabled = false;
    channel.onmessage = event => receive(event.data);
};

sendButton.onclick = async () => {
    const file = fileInput.files[0];
    const startedAt = performance.now();

    channel.send(JSON.stringify({ name: file.name, size: file.size }));

    for (let offset = 0; offset < file.size; offset += CHUNK_SIZE) {
        if (channel.bufferedAmount > 8 * MEGABYTE) {
            await new Promise(resolve => channel.onbufferedamountlow = resolve);
        }

        channel.send(await file.slice(offset, offset + CHUNK_SIZE).arrayBuffer());
        showProgress(Math.min(offset + CHUNK_SIZE, file.size), file.size, startedAt);
    }
};

function receive(data) {
    if (typeof data === "string") {
        incoming = { ...JSON.parse(data), chunks: [], received: 0, startedAt: performance.now() };
        return;
    }

    incoming.chunks.push(data);
    incoming.received += data.byteLength;
    showProgress(incoming.received, incoming.size, incoming.startedAt);

    if (incoming.received === incoming.size) {
        const link = document.createElement("a");
        link.href = URL.createObjectURL(new Blob(incoming.chunks));
        link.download = incoming.name;
        link.textContent = `${incoming.name} (${(incoming.size / MEGABYTE).toFixed(1)} МБ)`;
        link.className = "block text-indigo-600 underline";
        receivedList.append(link);
    }
}

// Прогресс и скорость передачи в МБ/с
function showProgress(done, total, startedAt) {
    const seconds = (performance.now() - startedAt) / 1000;

    progressBar.style.width = `${done / total * 100}%`;
    progressText.textContent = `${(done / MEGABYTE).toFixed(1)} / ${(total / MEGABYTE).toFixed(1)} МБ · ${(done / MEGABYTE / seconds).toFixed(1)} МБ/с`;
}
