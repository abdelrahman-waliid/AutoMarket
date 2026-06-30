# Swagger API Testing Guide

## 🚀 Quick Start

### 1. Run the API
```bash
cd Back/CarMarketplace.API
dotnet run
```

Or press **F5** in Visual Studio.

### 2. Access Swagger UI
Navigate to: **https://localhost:7110/swagger** or **http://localhost:5127/swagger**

---

## 📋 Testing Checklist

### ✅ UsersController Tests

#### 1. Register a New User
- **Endpoint**: `POST /api/users/register`
- **Request Body**:
```json
{
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "password": "password123",
  "role": 0
}
```
- **Expected**: 201 Created with UserDTO (no password)
- **Note**: Role values: 0=User, 1=Seller, 2=Admin

#### 2. Login User (Get JWT Token)
- **Endpoint**: `POST /api/users/login`
- **Request Body**:
```json
{
  "email": "john.doe@example.com",
  "password": "password123"
}
```
- **Expected**: 200 OK with UserDTO + JWT Token
- **Action**: Copy the `token` from response for JWT authentication

#### 3. Use JWT Token in Swagger
- Click the **"Authorize"** button (🔒) at the top of Swagger UI
- Enter: `Bearer {your_token_here}` (include "Bearer " prefix)
- Click "Authorize" then "Close"
- Now all protected endpoints will use this token

#### 4. Get All Users
- **Endpoint**: `GET /api/users`
- **Expected**: 200 OK with list of users

#### 5. Get User by ID
- **Endpoint**: `GET /api/users/{id}`
- **Replace {id}** with a user GUID from previous response
- **Expected**: 200 OK with UserDTO

#### 6. Update User
- **Endpoint**: `PUT /api/users/{id}`
- **Request Body**:
```json
{
  "id": "{same-id-as-url}",
  "fullName": "John Updated",
  "email": "john.updated@example.com",
  "role": 1
}
```
- **Expected**: 200 OK with updated UserDTO

#### 7. Delete User
- **Endpoint**: `DELETE /api/users/{id}`
- **Expected**: 204 No Content

---

### ✅ CarsController Tests

#### 1. Create a Car
- **Endpoint**: `POST /api/cars`
- **Request Body**:
```json
{
  "title": "2020 Toyota Camry",
  "description": "Excellent condition, low mileage",
  "price": 25000.00,
  "year": 2020,
  "mileage": 30000,
  "fuelType": 0,
  "transmissionType": 1,
  "ownerId": "{user-id-from-register}",
  "imageUrls": [
    "https://example.com/car1.jpg",
    "https://example.com/car2.jpg"
  ]
}
```
- **FuelType**: 0=Gasoline, 1=Diesel, 2=Electric, 3=Hybrid, 4=PlugInHybrid, 5=CNG
- **TransmissionType**: 0=Manual, 1=Automatic, 2=CVT, 3=SemiAutomatic
- **Expected**: 201 Created with CarDTO

#### 2. Get All Cars
- **Endpoint**: `GET /api/cars`
- **Expected**: 200 OK with list of cars

#### 3. Get Car by ID
- **Endpoint**: `GET /api/cars/{id}`
- **Expected**: 200 OK with CarDTO

#### 4. Search Cars
- **Endpoint**: `GET /api/cars/search?title=Toyota&minYear=2018&maxPrice=30000`
- **Query Parameters** (all optional):
  - `title`: Filter by title
  - `minYear`: Minimum year
  - `maxPrice`: Maximum price
- **Expected**: 200 OK with filtered list

#### 5. Predict Car Price
- **Endpoint**: `POST /api/cars/predict`
- **Request Body**: Same as Create Car (CarDTO)
- **Expected**: 200 OK with predicted price (decimal)

#### 6. Update Car
- **Endpoint**: `PUT /api/cars/{id}`
- **Request Body**: Updated CarDTO (same structure as create)
- **Expected**: 200 OK with updated CarDTO

#### 7. Delete Car
- **Endpoint**: `DELETE /api/cars/{id}`
- **Expected**: 204 No Content

---

### ✅ MessagesController Tests

#### 1. Send a Message
- **Endpoint**: `POST /api/messages`
- **Request Body**:
```json
{
  "senderId": "{user1-id}",
  "receiverId": "{user2-id}",
  "content": "Hello! Is this car still available?"
}
```
- **Expected**: 201 Created with MessageDTO
- **Note**: Create 2 users first to test messaging

#### 2. Get Messages for User
- **Endpoint**: `GET /api/messages/user/{userId}`
- **Expected**: 200 OK with all messages (sent + received)

#### 3. Get Messages Between Users
- **Endpoint**: `GET /api/messages/between/{user1Id}/{user2Id}`
- **Expected**: 200 OK with conversation messages

#### 4. Delete Message
- **Endpoint**: `DELETE /api/messages/{id}`
- **Expected**: 204 No Content

---

### ✅ AIController Tests

#### 1. Price Estimate (AI)
- **Endpoint**: `POST /api/ai/price-estimate`
- **Request Body**:
```json
{
  "brand": "Toyota",
  "model": "Corolla",
  "year": 2020,
  "mileage": 85000,
  "condition": "good",
  "transmission": "automatic",
  "fuelType": "gasoline",
  "location": "Cairo",
  "userPrice": 650000
}
```
- **Expected**: 200 OK with price estimate payload
- **Note**: If `userPrice` is omitted/null, the API returns estimate only (no evaluation).

---

## 🔐 JWT Authentication Testing

### Steps:
1. Register a user → Get user ID
2. Login with credentials → Get JWT token
3. Click "Authorize" button in Swagger UI
4. Enter: `Bearer {your_jwt_token}`
5. Click "Authorize"
6. Test protected endpoints (if any are protected)

### Token Format:
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🐛 Troubleshooting

### Swagger Not Loading?
- Check if API is running (look for "Now listening on..." in console)
- Verify URL: `https://localhost:7110/swagger` or `http://localhost:5127/swagger`
- Check browser console for errors

### 401 Unauthorized?
- Make sure you've logged in and copied the JWT token
- Use "Authorize" button in Swagger UI
- Format: `Bearer {token}` (include "Bearer " prefix)

### 400 Bad Request?
- Check request body format (JSON)
- Verify required fields are present
- Check data types match (e.g., enums are numbers)

### 404 Not Found?
- Verify the ID exists in the database
- Check the endpoint URL is correct

### Database Connection Error?
- Ensure SQL Server LocalDB is running
- Check connection string in `appsettings.json`
- Run migrations if needed

---

## 📊 Expected Endpoints Summary

### CarsController (7 endpoints)
- GET /api/cars
- GET /api/cars/{id}
- POST /api/cars
- PUT /api/cars/{id}
- DELETE /api/cars/{id}
- GET /api/cars/search
- POST /api/cars/predict

### UsersController (6 endpoints)
- POST /api/users/register
- POST /api/users/login
- GET /api/users
- GET /api/users/{id}
- PUT /api/users/{id}
- DELETE /api/users/{id}

### MessagesController (4 endpoints)
- GET /api/messages/user/{userId}
- GET /api/messages/between/{user1Id}/{user2Id}
- POST /api/messages
- DELETE /api/messages/{id}

### AIController (1 endpoint)
- POST /api/ai/price-estimate

**Total: 18 endpoints**

---

## ✅ Success Criteria

- [ ] All 18 endpoints appear in Swagger UI
- [ ] Can register and login users
- [ ] JWT token works via Authorize button
- [ ] Can create, read, update, delete cars
- [ ] Can search cars with filters
- [ ] Can predict car prices
- [ ] Can send and retrieve messages
- [ ] All endpoints return proper HTTP status codes
- [ ] Error handling works (400, 404, etc.)

---

## 🎯 Testing Order Recommendation

1. **Users**: Register → Login (get token) → Authorize in Swagger
2. **Cars**: Create → Get All → Get by ID → Search → Predict → Update → Delete
3. **Messages**: Send → Get for User → Get Between Users → Delete
4. **AI**: Price Estimate

Happy Testing! 🚀
