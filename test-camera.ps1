$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Basic YWRtaW46c2VjcmV0"
}
$body = Get-Content -Path "examples/exampleCameraFile.json" -Raw
Invoke-WebRequest -Uri "http://localhost:5000/api/camera" -Method POST -Headers $headers -Body $body -UsebasicParsing
