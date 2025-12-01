{
"info": {
"name": "Rule Engine Transaction Load Test",
"_postman_id": "11111111-2222-3333-4444-555555555555",
"description": "Sends randomized transaction requests to the local API for rule engine evaluation.",
"schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
},
"item": [
{
"name": "Random Transaction",
"request": {
"method": "POST",
"header": [
{
"key": "Content-Type",
"value": "application/json"
}
],
"url": {
"raw": "http://localhost:5000/api/Transactions",
"protocol": "http",
"host": [
"localhost"
],
"port": "5000",
"path": [
"api",
"Transactions"
]
},
"body": {
"mode": "raw",
"raw": "{\n "accountId": "{{accountId}}",\n "amount": {{amount}},\n "merchantId": "{{merchantId}}",\n "currency": "{{currency}}",\n "timestamp": "{{timestamp}}",\n "externalId": "{{externalId}}",\n "metadata": {\n "Country": "{{country}}",\n "IPAddress": "{{ipAddress}}"\n }\n}"
}
},
"event": [
{
"listen": "prerequest",
"script": {
"type": "text/javascript",
"exec": [
"// Helper: GUID generator",
"function uuidv4() {",
" return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {",
" var r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);",
" return v.toString(16);",
" });",
"}",
"",
"// Random GUIDs",
"pm.variables.set("accountId", uuidv4());",
"pm.variables.set("merchantId", uuidv4());",
"",
"// Amount variation (e.g. 1 – 10 000 with decimals)",
"const base = Math.random() * 10000;",
"const amount = Math.round(base * 100) / 100;",
"pm.variables.set("amount", amount);",
"",
"// Currency and country variation (bias to ZAR/RSA)",
"const currencies = ["ZAR", "RSA", "USD", "EUR", "GBP"];",
"const countries = ["ZAR", "RSA", "NA", "US", "GB", "DE"];",
"",
"function pickWeighted(values, primaryValues, primaryWeight) {",
" if (Math.random() < primaryWeight) {",
" return primaryValues[Math.floor(Math.random() * primaryValues.length)];",
" }",
" return values[Math.floor(Math.random() * values.length)];",
"}",
"",
"const currency = pickWeighted(currencies, ["ZAR", "RSA"], 0.7);",
"const country = pickWeighted(countries, ["ZAR", "RSA"], 0.7);",
"pm.variables.set("currency", currency);",
"pm.variables.set("country", country);",
"",
"// Timestamp = now (UTC ISO string)",
"pm.variables.set("timestamp", new Date().toISOString());",
"",
"// ExternalId with varying formats",
"const formats = [",
" () => txn-${Math.floor(Math.random() * 1000000)},",
" () => TXN_${Date.now()}_${Math.floor(Math.random() * 1000)},",
" () => ORD-${new Date().toISOString().replace(/[-:.TZ]/g, '')},",
" () => PAY-${Math.floor(Math.random() * 99999999)},",
" () => R${Math.floor(Math.random() * 1000000)}-${Date.now()}",
"];",
"const externalIdFunc = formats[Math.floor(Math.random() * formats.length)];",
"pm.variables.set("externalId", externalIdFunc());",
"",
"// Random internal IP range variation",
"const ip = 10.${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)}.${Math.floor(Math.random() * 256)};",
"pm.variables.set("ipAddress", ip);"
]
}
}
]
}
]
}