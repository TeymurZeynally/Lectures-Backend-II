const messagesElement = document.getElementById("messages");
const nicknameElement = document.getElementById("nickname");
const form = document.getElementById("form");
const input = document.getElementById("text");
const template = document.getElementById("message-template");
let nickname = null;

function render(message) {
    const element = template.content.firstElementChild.cloneNode(true);
    const isMine = message.author === nickname;

    element.querySelector("[data-author]").textContent = message.author;
    element.querySelector("[data-time]").textContent = new Date(message.sentAt).toLocaleTimeString();
    element.querySelector("[data-text]").textContent = message.text;
    element.classList.add(isMine ? "items-end" : "items-start");
    element.querySelector("[data-text]").classList.add(...(isMine ? ["bg-indigo-600", "text-white"] : ["bg-slate-100"]));

    messagesElement.append(element);
    messagesElement.scrollTop = messagesElement.scrollHeight;
}

const history = await fetch("/chat/messages").then(response => response.json());
history.forEach(render);

const socket = new WebSocket(`${location.protocol === "https:" ? "wss" : "ws"}://${location.host}/chat/ws`);

socket.onmessage = event => {
    const chatEvent = JSON.parse(event.data);

    if (chatEvent.type === "welcome") {
        nickname = chatEvent.nickname;
        nicknameElement.textContent = `Вы: ${nickname}`;
    }

    if (chatEvent.type === "message") {
        render(chatEvent.message);
    }
};

socket.onclose = () => nicknameElement.textContent = "соединение потеряно";

form.onsubmit = event => {
    event.preventDefault();
    if (!input.value.trim()) {
        return;
    }

    socket.send(JSON.stringify({ text: input.value }));
    input.value = "";
};
