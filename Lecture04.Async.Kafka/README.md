# Lecture 04 — Kafka

A minimal Kafka example: one **Producer** that floods a topic with "cat fed"
events, and a **Consumer** that you run as one or more instances of the same
service to see how a consumer group shares the work.

## Concepts shown

- **Topics & partitions** — the topic `cat-feedings` is auto-created with 6
  partitions (the broker's `KAFKA_NUM_PARTITIONS` default, set in
  `docker-compose.yml`).
- **Keyed ordering** — each message is keyed by `CatId`, so all feedings for
  the same cat always land on the same partition and stay in order.
- **Consumer groups & scaling** — every Consumer instance started with the
  same `GroupId` joins one group. Kafka splits the 6 partitions between
  however many instances are alive, and rebalances live as you add or stop
  them. Each instance logs `assigned partitions [...]` when this happens.
- **Backlog / throughput** — `ProcessingDelayMs` simulates per-message work.
  With one consumer it can't keep up with the producer and falls behind
  (watch the consumer lag grow in Kafka UI); start more consumer instances
  and the backlog drains faster because the partitions get split between
  them.

## 1. Start the broker

```bash
docker compose up -d
```

This starts:
- **Kafka** (KRaft mode, no Zookeeper needed) on `localhost:29092`
- **Kafka UI** on [http://localhost:8081](http://localhost:8081) — browse
  topics, partitions, consumer groups and lag from the browser

Everything works out of the box, no manual topic/setup steps required.

## 2. Run the Producer

```bash
cd Lecture04.Async.Kafka.Producer
dotnet run
```

Publishes to `cat-feedings` at `MessagesPerSecond` (default 50) forever. The
topic is auto-created by the broker on first use.

Override settings via command-line args (they override `appsettings.json`):

```bash
# publish much faster, to build up a visible backlog
dotnet run -- --Kafka:MessagesPerSecond=2000
```

## 3. Run one or more Consumers

Each terminal below is a separate **consumer instance** in the same
**consumer group** (`Kafka:GroupId`). Give each one a distinct `ClientId` so
you can tell them apart in the logs:

```bash
cd Lecture04.Async.Kafka.Consumer
dotnet run -- --Kafka:ClientId=consumer-1
```

Open more terminals for more instances:

```bash
dotnet run -- --Kafka:ClientId=consumer-2
dotnet run -- --Kafka:ClientId=consumer-3
```

Watch the logs: when a new instance joins, you'll see `revoked partitions`
and `assigned partitions` lines as Kafka rebalances the 6 partitions across
however many consumers are now running. Stop one (Ctrl+C) and watch its
partitions get reassigned to the others.

Since there are only 6 partitions, more than 6 consumers in the same group
will sit idle — a good way to show that partition count is the hard limit on
parallelism within a group.

Other useful overrides:

```bash
# simulate slower per-message processing, to make a single consumer fall behind
dotnet run -- --Kafka:ProcessingDelayMs=300

# start a second, independent group that reads the whole topic on its own
dotnet run -- --Kafka:GroupId=analytics --Kafka:ClientId=analytics-1
```

## Shutting down

```bash
docker compose down
```
