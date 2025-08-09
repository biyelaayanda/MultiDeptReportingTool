# PowerShell script to add test reports with authentication

$baseUrl = "http://localhost:5111/api"

# Function to register a user
function Register-User {
    param(
        [string]$username,
        [string]$password,
        [string]$email,
        [string]$firstName,
        [string]$lastName,
        [string]$role,
        [int]$departmentId
    )
    
    try {
        $registerData = @{
            username = $username
            password = $password
            email = $email
            firstName = $firstName
            lastName = $lastName
            role = $role
            departmentId = $departmentId
        } | ConvertTo-Json
        
        Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -Body $registerData -ContentType "application/json"
        Write-Host "✅ Registered user: $username" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to register user: $username - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Register users
Register-User -username "executive" -password "Executive123!" -email "executive@example.com" -firstName "Executive" -lastName "User" -role "Executive" -departmentId 1
Register-User -username "ceo" -password "CEO123!" -email "ceo@example.com" -firstName "CEO" -lastName "User" -role "Executive" -departmentId 1

# Function to authenticate and get JWT token
function Get-AuthToken {
    param(
        [string]$username,
        [string]$password
    )
    
    try {
        $loginData = @{
            username = $username
            password = $password
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginData -ContentType "application/json"
        return $response.token
    }
    catch {
        Write-Host "Failed to authenticate: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to make authenticated HTTP POST request
function Add-Report {
    param(
        [hashtable]$reportData,
        [string]$token
    )
    
    try {
        $json = $reportData | ConvertTo-Json
        $headers = @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri "$baseUrl/reports" -Method Post -Body $json -Headers $headers
        Write-Host "✅ Added report: $($reportData.title)" -ForegroundColor Green
        return $response
    }
    catch {
        Write-Host "❌ Failed to add report: $($reportData.title) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "🔐 Authenticating..." -ForegroundColor Cyan

# Try to authenticate with admin user (assuming it exists)
$adminToken = Get-AuthToken -username "admin" -password "Admin123!"

if (-not $adminToken) {
    Write-Host "❌ Admin authentication failed. Trying with CEO credentials..." -ForegroundColor Yellow
    $adminToken = Get-AuthToken -username "ceo" -password "CEO123!"
}

if (-not $adminToken) {
    Write-Host "❌ Authentication failed. Please check credentials or create a user first." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Authentication successful!" -ForegroundColor Green
Write-Host "🚀 Adding Reports..." -ForegroundColor Cyan

# Reports data
$reports = @(
    @{ 
        title = "Q1 Financial Performance Review"
        reportType = "Quarterly"
        description = "Comprehensive analysis of Q1 financial metrics and performance indicators"
        departmentId = 1
        reportPeriodStart = "2025-01-01T00:00:00Z"
        reportPeriodEnd = "2025-03-31T00:00:00Z"
        comments = "Initial Q1 review"
    },
    @{ 
        title = "Marketing Campaign ROI Analysis"
        reportType = "Monthly"
        description = "Analysis of recent marketing campaigns and their return on investment"
        departmentId = 2
        reportPeriodStart = "2025-07-01T00:00:00Z"
        reportPeriodEnd = "2025-07-31T00:00:00Z"
        comments = "July marketing performance"
    },
    @{ 
        title = "IT Infrastructure Security Audit"
        reportType = "Annual"
        description = "Comprehensive security audit of current IT infrastructure"
        departmentId = 3
        reportPeriodStart = "2025-01-01T00:00:00Z"
        reportPeriodEnd = "2025-12-31T00:00:00Z"
        comments = "Annual security review"
    },
    @{ 
        title = "HR Employee Satisfaction Survey"
        reportType = "Semi-Annual"
        description = "Employee satisfaction survey results and analysis"
        departmentId = 4
        reportPeriodStart = "2025-01-01T00:00:00Z"
        reportPeriodEnd = "2025-06-30T00:00:00Z"
        comments = "H1 2025 employee survey"
    },
    @{ 
        title = "Operations Efficiency Report"
        reportType = "Monthly"
        description = "Monthly operations efficiency metrics and improvement recommendations"
        departmentId = 1
        reportPeriodStart = "2025-07-01T00:00:00Z"
        reportPeriodEnd = "2025-07-31T00:00:00Z"
        comments = "July operations review"
    }
)

foreach ($report in $reports) {
    Add-Report -reportData $report -token $adminToken
    Start-Sleep -Milliseconds 200
}
