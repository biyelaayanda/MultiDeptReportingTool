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
Write-Host "🚀 Adding 25 Admin Reports..." -ForegroundColor Cyan

# Admin Reports (25 total)
$adminReports = @(
    @{ 
        title = "Q1 Financial Performance Review"
        reportType = "Quarterly"
        description = "Comprehensive analysis of Q1 financial metrics and performance indicators"
        departmentId = 1
        reportPeriodStart = (Get-Date "2025-01-01")
        reportPeriodEnd = (Get-Date "2025-03-31")
        comments = "Initial Q1 review"
    },
    @{ title = "Marketing Campaign ROI Analysis"; type = "Marketing"; description = "Analysis of recent marketing campaigns and their return on investment"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 1; dueDate = "2025-08-20T00:00:00Z" },
    @{ title = "IT Infrastructure Security Audit"; type = "Security"; description = "Comprehensive security audit of current IT infrastructure"; status = "Pending"; priority = "High"; departmentId = 3; assignedUserId = 1; dueDate = "2025-08-18T00:00:00Z" },
    @{ title = "HR Employee Satisfaction Survey"; type = "HR"; description = "Annual employee satisfaction survey results and analysis"; status = "Pending"; priority = "Medium"; departmentId = 4; assignedUserId = 1; dueDate = "2025-08-25T00:00:00Z" },
    @{ title = "Operations Efficiency Report"; type = "Operations"; description = "Monthly operations efficiency metrics and improvement recommendations"; status = "Pending"; priority = "Medium"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-22T00:00:00Z" },
    @{ title = "Sales Performance Dashboard"; type = "Sales"; description = "Weekly sales performance metrics and trend analysis"; status = "Pending"; priority = "High"; departmentId = 2; assignedUserId = 1; dueDate = "2025-08-16T00:00:00Z" },
    @{ title = "Customer Service Quality Review"; type = "Quality"; description = "Customer service quality metrics and improvement strategies"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 1; dueDate = "2025-08-28T00:00:00Z" },
    @{ title = "Product Development Milestone Review"; type = "Development"; description = "Review of product development milestones and timeline adjustments"; status = "Pending"; priority = "High"; departmentId = 3; assignedUserId = 1; dueDate = "2025-08-19T00:00:00Z" },
    @{ title = "Budget Allocation Analysis"; type = "Financial"; description = "Analysis of budget allocation across departments and projects"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-17T00:00:00Z" },
    @{ title = "Training Program Effectiveness"; type = "Training"; description = "Evaluation of training program effectiveness and participant feedback"; status = "Pending"; priority = "Medium"; departmentId = 4; assignedUserId = 1; dueDate = "2025-08-30T00:00:00Z" },
    @{ title = "Compliance Audit Report"; type = "Compliance"; description = "Regulatory compliance audit findings and corrective actions"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-14T00:00:00Z" },
    @{ title = "Digital Transformation Progress"; type = "Technology"; description = "Progress report on digital transformation initiatives"; status = "Pending"; priority = "Medium"; departmentId = 3; assignedUserId = 1; dueDate = "2025-08-26T00:00:00Z" },
    @{ title = "Market Research Analysis"; type = "Research"; description = "Market research findings and strategic recommendations"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 1; dueDate = "2025-08-24T00:00:00Z" },
    @{ title = "Supply Chain Optimization"; type = "Operations"; description = "Supply chain analysis and optimization recommendations"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-21T00:00:00Z" },
    @{ title = "Employee Performance Metrics"; type = "HR"; description = "Quarterly employee performance review and metrics analysis"; status = "Pending"; priority = "Medium"; departmentId = 4; assignedUserId = 1; dueDate = "2025-08-27T00:00:00Z" },
    @{ title = "Cybersecurity Threat Assessment"; type = "Security"; description = "Current cybersecurity threat landscape and mitigation strategies"; status = "Pending"; priority = "High"; departmentId = 3; assignedUserId = 1; dueDate = "2025-08-13T00:00:00Z" },
    @{ title = "Revenue Growth Analysis"; type = "Financial"; description = "Analysis of revenue growth trends and forecasting"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-23T00:00:00Z" },
    @{ title = "Customer Retention Strategy"; type = "Marketing"; description = "Customer retention analysis and strategic recommendations"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 1; dueDate = "2025-08-29T00:00:00Z" },
    @{ title = "Process Improvement Report"; type = "Operations"; description = "Process improvement initiatives and efficiency gains"; status = "Pending"; priority = "Medium"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-31T00:00:00Z" },
    @{ title = "Technology Stack Assessment"; type = "Technology"; description = "Assessment of current technology stack and upgrade recommendations"; status = "Pending"; priority = "Medium"; departmentId = 3; assignedUserId = 1; dueDate = "2025-09-05T00:00:00Z" },
    @{ title = "Workplace Safety Audit"; type = "Safety"; description = "Workplace safety audit findings and improvement recommendations"; status = "Pending"; priority = "High"; departmentId = 4; assignedUserId = 1; dueDate = "2025-08-12T00:00:00Z" },
    @{ title = "Competitive Analysis Report"; type = "Research"; description = "Competitive landscape analysis and market positioning"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 1; dueDate = "2025-09-02T00:00:00Z" },
    @{ title = "Cost Reduction Initiative"; type = "Financial"; description = "Cost reduction opportunities and implementation strategy"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-11T00:00:00Z" },
    @{ title = "Customer Feedback Analysis"; type = "Quality"; description = "Customer feedback analysis and service improvement recommendations"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 1; dueDate = "2025-09-03T00:00:00Z" },
    @{ title = "Strategic Planning Review"; type = "Strategic"; description = "Annual strategic planning review and goal assessment"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 1; dueDate = "2025-08-10T00:00:00Z" }
)

foreach ($report in $adminReports) {
    Add-Report -reportData $report -token $adminToken
    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "🔥 Adding 12 Executive Reports..." -ForegroundColor Cyan

# Executive Reports (12 total)
$executiveReports = @(
    @{ title = "Executive Dashboard Metrics"; type = "Executive"; description = "Executive-level KPI dashboard and performance metrics"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-15T00:00:00Z" },
    @{ title = "Board Meeting Preparation"; type = "Executive"; description = "Quarterly board meeting preparation and presentation materials"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-18T00:00:00Z" },
    @{ title = "Corporate Strategy Review"; type = "Strategic"; description = "Corporate strategy assessment and future planning"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-20T00:00:00Z" },
    @{ title = "Stakeholder Communication Plan"; type = "Communication"; description = "Stakeholder engagement and communication strategy"; status = "Pending"; priority = "Medium"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-22T00:00:00Z" },
    @{ title = "Risk Management Assessment"; type = "Risk"; description = "Enterprise risk management assessment and mitigation strategies"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-25T00:00:00Z" },
    @{ title = "Merger and Acquisition Analysis"; type = "Strategic"; description = "M and A opportunities and strategic partnership analysis"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-28T00:00:00Z" },
    @{ title = "Investor Relations Update"; type = "Financial"; description = "Investor relations quarterly update and communication"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-08-30T00:00:00Z" },
    @{ title = "Leadership Development Program"; type = "Leadership"; description = "Executive leadership development program assessment"; status = "Pending"; priority = "Medium"; departmentId = 4; assignedUserId = 2; dueDate = "2025-09-02T00:00:00Z" },
    @{ title = "Corporate Governance Review"; type = "Governance"; description = "Corporate governance framework review and compliance"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-09-05T00:00:00Z" },
    @{ title = "Market Expansion Strategy"; type = "Strategic"; description = "Market expansion opportunities and strategic planning"; status = "Pending"; priority = "Medium"; departmentId = 2; assignedUserId = 2; dueDate = "2025-09-08T00:00:00Z" },
    @{ title = "Digital Innovation Roadmap"; type = "Innovation"; description = "Digital innovation strategy and technology roadmap"; status = "Pending"; priority = "Medium"; departmentId = 3; assignedUserId = 2; dueDate = "2025-09-10T00:00:00Z" },
    @{ title = "Annual Performance Review"; type = "Performance"; description = "Annual organizational performance review and planning"; status = "Pending"; priority = "High"; departmentId = 1; assignedUserId = 2; dueDate = "2025-09-15T00:00:00Z" }
)

foreach ($report in $executiveReports) {
    Add-Report -reportData $report -token $adminToken
    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "✨ Test data creation complete!" -ForegroundColor Green
Write-Host "📊 Total Reports Added:" -ForegroundColor Yellow
Write-Host "  • Admin Reports: 25" -ForegroundColor White
Write-Host "  • Executive Reports: 12" -ForegroundColor White
Write-Host "  • Total: 37 reports" -ForegroundColor White
Write-Host ""
Write-Host "🔍 Now test the pagination with:" -ForegroundColor Cyan
Write-Host "  • Admin role: Should show 25 reports (3 pages: 10+10+5)" -ForegroundColor White
Write-Host "  • Executive role: Should show 12 reports (2 pages: 10+2)" -ForegroundColor White
