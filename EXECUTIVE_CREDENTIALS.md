# Executive Dashboard Credentials

## Phase 4 Analytics & Dashboard - Executive Users

### 🎯 **Executive User 1**
- **Username:** `executive`
- **Password:** `Executive123!`
- **Email:** `executive@example.com`
- **Role:** Executive
- **Department:** Finance (ID: 1)
- **User ID:** 8

### 🎯 **CEO User**
- **Username:** `ceo`
- **Password:** `CEO123!`
- **Email:** `ceo@example.com`
- **Role:** Executive
- **Department:** Finance (ID: 1)
- **User ID:** 9

### 🎯 **Admin User (Already exists)**
- **Username:** `admin`
- **Password:** `Admin123!`
- **Email:** `admin@example.com`
- **Role:** Admin
- **Department:** Finance (ID: 1)
- **User ID:** 2

## 🚀 How to Test the Executive Dashboard

### 1. **Login Process**
```json
POST http://localhost:5111/api/auth/login
Content-Type: application/json

{
  "username": "executive",
  "password": "Executive123!"
}
```

### 2. **Access Analytics APIs**
Use the returned JWT token in the Authorization header:
```
Authorization: Bearer [your-jwt-token]
```

### 3. **Key Executive Dashboard Endpoints**
- `GET /api/analytics/executive-dashboard` - Complete dashboard data
- `GET /api/analytics/business-intelligence` - BI insights
- `GET /api/analytics/kpi-metrics` - Key performance indicators
- `GET /api/export/supported-formats` - Available export formats

### 4. **Frontend Access**
Once the Angular app is running:
- Navigate to `/executive` route
- Login with executive credentials
- View the comprehensive analytics dashboard

## 📊 Dashboard Features Available

### **Executive Dashboard Includes:**
✅ **Company Overview** - Total reports, completion rates, departments, users
✅ **KPI Metrics** - 6 key performance indicators with trends
✅ **Department Performance** - Completion rates by department
✅ **Critical Alerts** - Real-time system alerts
✅ **Top Performers** - Best performing users
✅ **System Health** - Overall system status
✅ **Business Intelligence** - Insights, recommendations, predictions
✅ **Recent Trends** - Historical performance data
✅ **Export Capabilities** - PDF, Excel, PowerPoint exports

### **Export Features:**
✅ **Multi-format Exports** - PDF, Excel, CSV, JSON, PowerPoint, Word
✅ **Email Notifications** - Send reports via email
✅ **Scheduled Reports** - Automated report generation
✅ **Chart Generation** - Interactive visualizations

## 🔐 Authentication Flow
1. Login with executive credentials
2. Receive JWT token (valid for 1 hour)
3. Use token for all API calls
4. Frontend automatically handles authentication

## 🎨 Frontend Dashboard
The Angular executive dashboard provides:
- Real-time data visualization
- Interactive charts and graphs
- Export functionality
- Responsive design
- Auto-refresh capabilities
- Critical alerts monitoring

**Ready to test your complete Phase 4 Analytics & Dashboard implementation!** 🚀
