### Формат конфига

BotLooter.Config.json

```json
{
  "LootTradeOfferUrl": "",
  
  "SecretsDirectoryPath": "Secrets",
  "AccountsFilePath": "Accounts.txt",
  "ProxiesFilePath": "Proxies.txt",
  
  "DelayBetweenAccountsSeconds": 30,
  
  "DelayInventoryEmptySeconds ": 10,
  
  "AskForApproval": true,
  
  "LootThreadCount": 1
}
```

- `LootTradeOfferUrl` - ссылка на трейд оффер, на который будет отправляться лут
- `SecretsDirectoryPath` - путь к папке с мафайлами
- `AccountsFilePath` - путь к файлу с аккаунтами формата username:password
- `ProxiesFilePath` - путь к файлу с прокси формата 'protocol://username:password@address:port' или 'protocol://address:port'
- `DelayBetweenAccountsSeconds` - задержка между аккаунтами в секундах
- `DelayInventoryEmptySeconds ` - задержка при пустом инвентаре в секундах
- `AskForApproval ` - При значении true, для продолжения будет требоваться нажать любую клавишу, а при значении false, будет 5 секундное ожидание.
- `LootThreadCount` - Максимальное количество потоков для лутания.

### Функционал

- Возможность лутать инвентари CS:GO на одну трейд ссылку (ссылка указывается в конфиге)
- Для лутания используются прокси, 1 на аккаунт, по кругу (таким образом все прокси используются равномерно)
- Можно не использовать прокси, для этого установите `ProxiesFilePath` пустое значение `""`

### Примеры

![Скриншот работы софта](Assets/Screenshot.png)