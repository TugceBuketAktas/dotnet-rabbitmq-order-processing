# RabbitMQ Sipariş İşleme Sistemi (Order Processing System)

Mini mikroservis demosu - .NET Core + React + RabbitMQ (tamamen Docker ile)

## 🏗️ Proje Yapısı

```
├── OrderModels/          # Shared DTOs and Models
│   ├── Models/           # Order, OrderItem
│   ├── DTOs/             # CreateOrderDto, OrderDto, OrderItemDto
│   └── Enums/            # OrderStatus
│
├── OrderApi/             # ASP.NET Core Web API
│   ├── Controllers/      # OrdersController
│   ├── Services/         # RabbitMQProducer
│   ├── Repositories/     # OrderRepository, IOrderRepository
│   └── Data/             # OrderDbContext (In-Memory)
│
├── OrderWorker/          # Background Worker Service
│   ├── Consumers/        # RabbitMQConsumer
│   └── Services/         # OrderProcessingService
│
├── order-client/         # React + Vite Frontend
│   ├── src/
│   │   ├── components/   # OrderForm, OrderStatus
│   │   ├── styles/       # CSS files
│   │   └── App.jsx       # Main app
│
└── docker-compose.yml    # RabbitMQ container
```

## 🚀 Başlangıç (Docker)

### Gereksinimler

- **Docker Desktop** (Compose dahil) - [İndir](https://www.docker.com/products/docker-desktop)

### 1. Tüm Servisleri Tek Komutla Başlat

```bash
docker compose up --build -d
```

### 2. Servisleri Kontrol Et

```bash
docker compose ps
docker compose logs -f
```

### 3. Erişim Noktaları

- **Frontend**: http://localhost:5173
- **API**: http://localhost:5080
- **RabbitMQ UI**: http://localhost:15673
  - Kullanıcı: `guest`
  - Şifre: `guest`

## 📋 API Endpoints

### POST /api/orders
Yeni sipariş oluştur

**Request:**
```json
{
  "customerId": 1,
  "items": [
    {
      "productId": 101,
      "quantity": 2,
      "price": 29.99
    },
    {
      "productId": 102,
      "quantity": 1,
      "price": 49.99
    }
  ]
}
```

**Response (201 Created):**
```json
{
  "orderId": 1,
  "customerId": 1,
  "totalAmount": 109.97,
  "createdAt": "2026-04-24T20:40:00Z",
  "items": [...]
}
```

### GET /api/orders/{id}
Sipariş detaylarını getir

**Response (200 OK):**
```json
{
  "orderId": 1,
  "customerId": 1,
  "totalAmount": 109.97,
  "createdAt": "2026-04-24T20:40:00Z",
  "items": [...]
}
```

### GET /api/orders
Tüm siparişleri listele

## 🔄 İş Akışı

1. **Kullanıcı** React form'dan sipariş gönderir
2. **API** siparişi veritabanına ve outbox kaydına yazar
3. **Outbox Publisher** siparişi RabbitMQ queue'ya güvenli şekilde yayınlar
4. **Consumer** queue'dan mesaj alır
5. **Worker** siparişi işler (validasyon, ödeme, vs.)
6. **Kullanıcı** status sayfasından sonucu kontrol eder

## 📊 Order Statüsleri

- **0: Bekleniyor (Pending)** - Yeni oluşturulan sipariş
- Outbox tarafında yayın bekliyor veya worker'a henüz ulaşmadı
- **1: İşleniyor (Processing)** - Worker tarafından işlenmekte
- **2: Tamamlandı (Completed)** - Başarıyla tamamlanan
- **3: Başarısız (Failed)** - Hata oluşan

## 🔧 Konfigürasyon

### OrderApi/appsettings.json
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### OrderWorker/appsettings.json
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

## 📝 Örnek Test

### 1. Yeni Sipariş Oluştur
```bash
curl -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "items": [
      {"productId": 1, "quantity": 5, "price": 15.50},
      {"productId": 2, "quantity": 3, "price": 25.00}
    ]
  }'
```

### 2. Sipariş Durumunu Kontrol Et
```bash
curl http://localhost:5080/api/orders/1
```

## 📚 Teknolojiler

- **Backend**: ASP.NET Core 9.0
- **Frontend**: React 19 + Vite
- **Queue**: RabbitMQ
- **Database**: Entity Framework Core (In-Memory)
- **Container**: Docker

## 🧹 Durdurma

```bash
docker compose down
```

## 🎯 Öğrenme Hedefleri

Bu demo aşağıdaki kavramları göstermektedir:

✅ **Asynchronous Processing** - Queue-based order handling  
✅ **Microservices Architecture** - Loose coupling between API & Worker  
✅ **Message Queue** - RabbitMQ pub/sub pattern  
✅ **REST API** - Standard CRUD operations  
✅ **React Frontend** - Simple UI for user interaction  
✅ **Background Services** - Long-running worker processes  
