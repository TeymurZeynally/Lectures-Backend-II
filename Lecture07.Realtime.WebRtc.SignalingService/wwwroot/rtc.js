// room  - имя комнаты на сигнальном сервере
// mode  - "stun" или "turn"
// setup - вызывается до начала обмена offer/answer: здесь страница добавляет свои треки или DataChannel, тобы они попали в SDP
export async function connect(room, mode, setup) {
    // Список STUN/TURN-серверов отдаёт наш сервер
    const iceServers = await fetch(`/signaling/ice-servers?mode=${mode}`).then(response => response.json());

    // RTCPeerConnection - само соединение между двумя браузерами
    // iceTransportPolicy "relay" запрещает прямые соединения: разрешены только кандидаты через TURN "all" - браузер пробует все варианты (host, srflx, relay) и выбирает лучший
    const peerConnection = new RTCPeerConnection({ iceServers, iceTransportPolicy: mode === "turn" ? "relay" : "all" });
    setup(peerConnection);

    // Сигнальный канал: WebSocket к нашему серверу. Через него браузеры обмениваются offer, answer и ICE-кандидатами. Сами медиа и данные через сервер не идут
    const socket = new WebSocket(`${location.protocol === "https:" ? "wss" : "ws"}://${location.host}/signaling/rooms/${room}`);
    const send = message => socket.send(JSON.stringify(message));

    // Браузер нашёл очередной адрес, по которому к нему можно подключиться (ICE-кандидат): сразу отправляем его второму участнику, не дожидаясь остальных 
    peerConnection.onicecandidate = event => event.candidate && send({ type: "candidate", candidate: event.candidate });

    async function handle(message) {
        // Мы были в комнате первыми, и пришёл второй участник - мы начинаем обмен.
        // setLocalDescription() без аргументов сам создаёт offer (SDP: треки, кодеки, ключи шифрования) и запускает сбор ICE-кандидатов
        if (message.type === "peer-joined") {
            await peerConnection.setLocalDescription();
            send({ type: "offer", description: peerConnection.localDescription });
        }

        // Мы пришли вторыми и получили offer: запоминаем описание собеседника и в ответ создаём answer (setLocalDescription после offer создаёт именно answer)
        if (message.type === "offer") {
            await peerConnection.setRemoteDescription(message.description);
            await peerConnection.setLocalDescription();
            send({ type: "answer", description: peerConnection.localDescription });
        }

        // Первый участник получил answer - обе стороны знают описания друг друга
        if (message.type === "answer") {
            await peerConnection.setRemoteDescription(message.description);
        }

        // Кандидат собеседника: браузер пробует установить соединение по этому адресу
        if (message.type === "candidate") {
            await peerConnection.addIceCandidate(message.candidate);
        }
    }

    // Сообщения обрабатываются строго по очереди: handle асинхронный, и без очереди кандидат
    // мог бы начать добавляться раньше, чем закончится setRemoteDescription для offer
    let queue = Promise.resolve();
    socket.onmessage = event => queue = queue.then(() => handle(JSON.parse(event.data)));

    return peerConnection;
}


export function showStatus(peerConnection, element) {
    element.textContent = "ожидание второго участника...";

    peerConnection.onconnectionstatechange = async () => {
        element.textContent = peerConnection.connectionState;

        if (peerConnection.connectionState === "connected") {
            const stats = await peerConnection.getStats();
            const pair = [...stats.values()].find(s => s.type === "candidate-pair" && s.nominated && s.state === "succeeded");
            const local = stats.get(pair.localCandidateId);
            const remote = stats.get(pair.remoteCandidateId);

            element.textContent += ` · ${local.candidateType} ${local.address}:${local.port} ⇄ ${remote.candidateType} ${remote.address}:${remote.port}`;
        }
    };
}
