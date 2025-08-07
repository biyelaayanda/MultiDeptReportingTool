# Simple script to add test reports
$baseUrl = "http://localhost:5111"

# Login and get token
$loginBody = @{
    username = "admin"
    password = "Admin123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

Write-Host "Token received: $($token.Substring(0, 20))..." -ForegroundColor Green

# Create headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Admin reports (25)
Write-Host "Adding 25 Admin reports..." -ForegroundColor Yellow
for ($i = 1; $i -le 25; $i++) {
    $startDate = (Get-Date).AddDays(-30)
    $endDate = (Get-Date).AddDays(-1)
    
    $reportType = switch ($i % 4) {
        0 { "Monthly" }
        1 { "Weekly" }
        2 { "Quarterly" }
        3 { "Annual" }
    }
    
    $report = @{
        title = "Admin Report $i"
        description = "Admin test report number $i for pagination testing"
        reportType = $reportType
        departmentId = 1
        reportPeriodStart = $startDate.ToString("yyyy-MM-ddTHH:mm:ss")
        reportPeriodEnd = $endDate.ToString("yyyy-MM-ddTHH:mm:ss")
        comments = "Test report for pagination validation"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/reports" -Method POST -Body $report -Headers $headers
        Write-Host "Created report $i" -ForegroundColor Green
    }
    catch {
        Write-Host "Error creating report $i`: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Executive reports (12) - Need executive user
Write-Host "Logging in as Executive..." -ForegroundColor Yellow
$execLoginBody = @{
    username = "executive"
    password = "Exec123!"
} | ConvertTo-Json

try {
    $execLoginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $execLoginBody -ContentType "application/json"
    $execToken = $execLoginResponse.token
    Write-Host "Executive token received" -ForegroundColor Green

    $execHeaders = @{
        "Authorization" = "Bearer $execToken"
        "Content-Type" = "application/json"
    }

    Write-Host "Adding 12 Executive reports..." -ForegroundColor Yellow
    for ($i = 1; $i -le 12; $i++) {
        $startDate = (Get-Date).AddDays(-60)
        $endDate = (Get-Date).AddDays(-31)
        
        $reportType = switch ($i % 3) {
            0 { "Quarterly" }
            1 { "Monthly" }
            2 { "Annual" }
        }
        
        $report = @{
            title = "Executive Report $i"
            description = "Executive test report number $i for pagination testing"
            reportType = $reportType
            departmentId = 1
            reportPeriodStart = $startDate.ToString("yyyy-MM-ddTHH:mm:ss")
            reportPeriodEnd = $endDate.ToString("yyyy-MM-ddTHH:mm:ss")
            comments = "Executive test report for pagination validation"
        } | ConvertTo-Json

        try {
            $response = Invoke-RestMethod -Uri "$baseUrl/api/reports" -Method POST -Body $report -Headers $execHeaders
            Write-Host "Created executive report $i" -ForegroundColor Green
        }
        catch {
            Write-Host "Error creating executive report $i`: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
catch {
    Write-Host "Could not login as executive, using admin for all reports..." -ForegroundColor Yellow
    Write-Host "Adding 12 more Admin reports..." -ForegroundColor Yellow
    for ($i = 26; $i -le 37; $i++) {
        $startDate = (Get-Date).AddDays(-90)
        $endDate = (Get-Date).AddDays(-61)
        
        $reportType = switch ($i % 4) {
            0 { "Monthly" }
            1 { "Weekly" }
            2 { "Quarterly" }
            3 { "Annual" }
        }
        
        $report = @{
            title = "Admin Report $i"
            description = "Additional admin test report number $i for pagination testing"
            reportType = $reportType
            departmentId = 1
            reportPeriodStart = $startDate.ToString("yyyy-MM-ddTHH:mm:ss")
            reportPeriodEnd = $endDate.ToString("yyyy-MM-ddTHH:mm:ss")
            comments = "Additional test report for pagination validation"
        } | ConvertTo-Json

        try {
            $response = Invoke-RestMethod -Uri "$baseUrl/api/reports" -Method POST -Body $report -Headers $headers
            Write-Host "Created additional report $i" -ForegroundColor Green
        }
        catch {
            Write-Host "Error creating additional report $i`: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "Test data creation completed!" -ForegroundColor Green
