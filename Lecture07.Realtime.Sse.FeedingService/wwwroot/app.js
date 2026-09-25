const feedersElement = document.getElementById("feeders");
const statusElement = document.getElementById("status");
const template = document.getElementById("feeder-template");
const cards = {};

function render(feeder) {
    if (!cards[feeder.id]) {
        const card = template.content.firstElementChild.cloneNode(true);
        card.querySelector("[data-refill]").onclick = () => fetch(`/feeders/${feeder.id}/refill`, { method: "POST" });
        feedersElement.append(card);
        cards[feeder.id] = card;
    }

    const card = cards[feeder.id];
    const percent = Math.round(feeder.foodGrams / feeder.capacityGrams * 100);
    const bar = card.querySelector("[data-bar]");

    card.querySelector("[data-name]").textContent = feeder.catName;
    card.querySelector("[data-grams]").textContent = `${feeder.foodGrams} / ${feeder.capacityGrams} г`;
    card.querySelector("[data-updated]").textContent = new Date(feeder.updatedAt).toLocaleTimeString();
    bar.style.width = `${percent}%`;
    bar.className = `h-full transition-all duration-500 ${percent < 25 ? "bg-red-500" : percent < 50 ? "bg-amber-400" : "bg-emerald-500"}`;
}

// EventSource opens GET /feeders/stream and reconnects automatically if the connection drops
const source = new EventSource("/feeders/stream");

source.addEventListener("feeder-updated", event => render(JSON.parse(event.data)));

source.onopen = () => {
    statusElement.textContent = "онлайн";
    statusElement.className = "px-3 py-1 rounded-full text-sm bg-emerald-100 text-emerald-700";
};

source.onerror = () => {
    statusElement.textContent = "переподключение...";
    statusElement.className = "px-3 py-1 rounded-full text-sm bg-red-100 text-red-700";
};
