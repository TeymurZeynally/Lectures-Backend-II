# Lecture 04 — RabbitMQ

A minimal RabbitMQ example: one **Producer** that floods a queue with "cat
fed" events, and a **Consumer** that you run as one or more instances to see
how RabbitMQ shares the work between them.

## Concepts shown

- **Exchange, routing key, queue** — the producer publishes to a `direct`
  exchange (`cat-feedings-exchange`) with routing key `feeding`; the queue
  `cat-feedings` is bound to that exchange with the same routing key. Both
  services declare this topology on startup, so it exists no matter which
  one you start first.
- **Competing consumers** — every Consumer instance you start subscribes to
  the *same* queue. Unlike Kafka, there's no partitioning to think about:
  RabbitMQ just dispatches each message to one of the connected consumers.
- **Prefetch / fair dispatch** — `PrefetchCount` (QoS) caps how many
  unacknowledged messages a consumer can hold at once. With the default of
  1, RabbitMQ won't hand a consumer a new message until it has acknowledged
  the previous one, so a busy consumer stops getting new work and it
  naturally spreads to whichever consumer is free. Raise it and you'll see
  dispatch become blind round-robin instead of load-based.
- **Manual ack** — the consumer acks each message only after "processing"
  it (`DelayMs`). If a consumer is killed before acking, RabbitMQ
  redelivers that message to another consumer instead of losing it.

## 1. Start the broker

```bash
docker compose up -d
```

This starts **RabbitMQ** with the management plugin built in:
- AMQP on `localhost:5672`
- Management UI on [http://localhost:15672](http://localhost:15672)
  (login `guest` / `guest`) — browse exchanges, queues, bindings and message
  rates from the browser

Everything works out of the box, no manual setup required.

## 2. Run the Producer

```bash
cd Lecture04.Async.RabbitMq.Producer
dotnet run
```

Publishes to `cat-feedings-exchange` at `MessagesPerSecond` (default 50)
forever. Declares the exchange, queue and binding on startup.

Override settings via command-line args (they override `appsettings.json`):

```bash
# publish much faster, to build up a visible backlog
dotnet run -- --Processing:MessagesPerSecond=2000
```

## 3. Run one or more Consumers

Each terminal below is a separate **consumer instance** reading from the
same queue. Give each one a distinct `ConsumerTag` so you can tell them
apart in the logs:

```bash
cd Lecture04.Async.RabbitMq.Consumer
dotnet run -- --Processing:ConsumerTag=consumer-1
```

Open more terminals for more instances:

```bash
dotnet run -- --Processing:ConsumerTag=consumer-2
dotnet run -- --Processing:ConsumerTag=consumer-3
```

With one consumer and a non-trivial `DelayMs`, it can't keep up
with the producer and falls behind (watch the queue's message count grow in
the management UI). Start a second instance and you'll see both consumers'
logs interleave on roughly-consecutive sequence numbers — the backlog gets
processed twice as fast because RabbitMQ is sharing it between them.

Other useful overrides:

```bash
# simulate slower per-message processing, to make a single consumer fall behind
dotnet run -- --Processing:DelayMs=300

# raise prefetch to see blind round-robin instead of load-based dispatch
dotnet run -- --Processing:PrefetchCount=50

# point at a different broker (connection settings live under RabbitMq, not Processing)
dotnet run -- --RabbitMq:HostName=my-broker
```

## Shutting down

```bash
docker compose down
```
