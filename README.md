# Smart Cart & Coupon Checkout – E-commerce Full Stack App

## Overview
A simplified e-commerce checkout system built with **.NET 8 Web API** (C#) and **React** (TypeScript). Customers can browse products, add items to a cart, apply coupons, and place orders with full pricing breakdown.

## Architecture

```
┌─────────────────┐     HTTP/JSON     ┌──────────────────────────────┐
│  React Frontend │ ◄──────────────► │    .NET 8 Web API            │
│  (TypeScript)   │                   │                              │
│                 │                   │  Controllers                 │
│  - ProductList  │                   │    ├── ProductsController    │
│  - CartPage     │                   │    ├── CartController        │
│  - OrderConfirm │                   │    └── OrdersController      │
│                 │                   │  Services (Business Logic)   │
│                 │                   │    ├── ProductService        │
│                 │                   │    ├── CartService            │
│                 │                   │    ├── CouponService          │
│                 │                   │    └── OrderService           │
│                 │                   │  Data (EF Core In-Memory)    │
└─────────────────┘                   └──────────────────────────────┘
```

**Design Pattern:** Clean layered architecture — Controller → Service → Repository (EF Core DbContext).

## Setup & Run

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- npm or yarn

### Backend
```bash
cd backend
dotnet restore
dotnet run --project SmartCartApi
# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### Frontend
```bash
cd frontend
npm install
npm run dev
# App available at http://localhost:3000
```

### Run Tests
```bash
cd backend
dotnet test
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List all products |
| POST | `/api/cart/items` | Add/update item in cart |
| GET | `/api/cart/{cartId}` | Get cart details |
| POST | `/api/cart/{cartId}/apply-coupon` | Apply coupon to cart |
| POST | `/api/cart/{cartId}/checkout` | Checkout cart and create order |
| GET | `/api/orders/{orderId}` | Get order details |

## Business Rules

### Coupon Rules
| Code | Type | Value | Min Subtotal | Max Discount |
|------|------|-------|-------------|--------------|
| FLAT50 | Flat discount | ₹50 | ₹500 | — |
| SAVE10 | Percentage | 10% | ₹1000 | ₹200 |

### Checkout
- Cart item quantity must be > 0
- Stock is validated before checkout
- Checkout is atomic (transaction-based): stock reduction + order creation
- Tax: 18% GST on subtotal after discount
- Order response includes full pricing breakdown

## Sample Data

### Products
| ID | Name | Price | Stock | Category |
|----|------|-------|-------|----------|
| 1 | Wireless Headphones | ₹299.99 | 50 | Electronics |
| 2 | Programming Book - C# | ₹149.99 | 100 | Books |
| 3 | Mechanical Keyboard | ₹199.99 | 30 | Electronics |
| 4 | USB-C Hub | ₹79.99 | 75 | Accessories |
| 5 | Monitor Stand | ₹129.99 | 40 | Accessories |
| 6 | Laptop Backpack | ₹89.99 | 60 | Accessories |
| 7 | Webcam HD | ₹159.99 | 25 | Electronics |
| 8 | Desk Lamp | ₹59.99 | 80 | Home Office |

## Tests
- **CouponServiceTests**: Validates flat and percentage discount calculation, min subtotal enforcement, inactive coupon rejection
- **CartServiceTests**: Stock validation, cart creation, quantity accumulation
- **OrderServiceTests**: Empty cart handling, pricing accuracy with coupons, stock reduction after checkout
- **IntegrationTests**: Full end-to-end checkout flow via HTTP

## Assumptions
1. EF Core In-Memory database is used (no SQL Server required)
2. Single-user scenario (no authentication)
3. Tax rate is fixed at 18% GST
4. Cart IDs are GUIDs generated server-side
5. Stock is validated at both add-to-cart and checkout time
6. Only one coupon can be applied per cart
