# Лекция 08 — Обнаружение сервисов с Consul

В предыдущих примерах список инстансов был прописан в `haproxy.cfg`
руками. Здесь его нет: каждый инстанс **CatsService** при старте сам
регистрируется в **Consul**, Consul сам проверяет его здоровье, а
**HAProxy** узнаёт актуальный список инстансов через DNS-интерфейс Consul.
Запустили новый инстанс с любым номером — он получает трафик, ничего не
правя.

## Концепции

- **Self-registration** — `ConsulRegistration` вызывает
  `PUT /v1/agent/service/register` (имя сервиса `cats`, адрес, порт и
  проверка здоровья) при старте и затем повторяет каждые 5 секунд — это
  heartbeat со слайда. При штатной остановке (Ctrl+C) —
  `PUT /v1/agent/service/deregister/cats-N`.
- **Health check делает Consul** — раз в секунду вызывает `GET /health`
  каждого инстанса. Инстанс, не прошедший проверку, остаётся в реестре,
  но больше не попадает в ответы на запросы.
- **Удаление по таймауту** — если проверка красная дольше
  `DeregisterCriticalServiceAfter`, Consul удаляет инстанс из реестра
  вместе с его проверкой. Минимум у Consul — 1 минута (меньшие значения
  он игнорирует), и удаление делается фоновым процессом, поэтому на
  практике это 1–1,5 минуты. Живой инстанс повторной регистрацией
  сбрасывает этот таймер и из реестра не пропадает — удаляются только
  упавшие процессы. Без повторной регистрации инстанс, удалённый по
  таймауту, в Consul бы уже не вернулся: Consul о нём больше не знает и
  не проверяет его.
- **Server-side discovery** — HAProxy раз в секунду спрашивает у Consul
  DNS-запись `_cats._tcp.service.consul` (SRV: адрес и порт каждого
  здорового инстанса) и заполняет ею 10 слотов `server-template`.
  Генератор по-прежнему знает только адрес HAProxy.
- **Client-side discovery** — режим генератора `ClientSide`: он сам раз в
  секунду спрашивает у Consul `GET /v1/health/service/cats?passing` и
  отправляет запросы напрямую инстансам, выбирая случайный. HAProxy в этом
  режиме не нужен.
- **DNS-запись можно посмотреть руками**:

  ```bash
  dig @127.0.0.1 -p 8600 _cats._tcp.service.consul SRV
  ```

## 1. Запуск Consul и HAProxy

```bash
docker compose up -d
```

- Consul UI: [http://localhost:8500](http://localhost:8500) — вкладка
  Services → `cats`, видно каждый инстанс и его проверку
- HAProxy: [http://localhost:8080](http://localhost:8080)
- Статистика HAProxy: [http://localhost:8404](http://localhost:8404) —
  слоты `cats1`...`cats10`, занятые слоты показывают адрес инстанса,
  пустые — в состоянии MAINT

Оба контейнера работают в `network_mode: host` (на Docker Desktop его
нужно включить в Settings → Resources → Network).

## 2. Запуск инстансов

```bash
cd Lecture08.ServiceDiscovery.Consul.CatsService
dotnet run -- --Instance:Number=1
dotnet run -- --Instance:Number=2
dotnet run -- --Instance:Number=3
```

(каждый в своей консоли). В логе каждого:
`Registered cats-N as service cats in Consul`.

## 3. Запуск генератора нагрузки

```bash
cd Lecture08.ServiceDiscovery.Consul.LoadGenerator
dotnet run -- --LoadGenerator:RequestsPerSecond=300
```

## Остановка

```bash
docker compose down
```
