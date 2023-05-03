### Формат конфига

BotLooter.Config.json

```json
{
  "LootTradeOfferUrl": "https://...",
  
  "SecretsDirectoryPath": "Secrets",
  
  "AccountsFilePath": "Accounts.txt",
  
  "ProxiesFilePath": "Proxies.txt",
  
  "DelayBetweenAccountsSeconds": 3
}
```

- `LootTradeOfferUrl` - ссылка на трейд оффер, на который будет отправляться лут
- `SecretsDirectoryPath` - путь к папке с мафайлами
- `AccountsFilePath` - путь к файлу с аккаунтами формата username:password
- `ProxiesFilePath` - путь к файлу с прокси формата protocol://username:password@address:port
- `DelayBetweenAccountsSeconds` - задержка между аккаунтами в секундах

### Функционал

- Возможность лутать инвентари CS:GO на одну трейд ссылку (ссылка указывается в конфиге)
- Для лутания используются прокси, 1 на аккаунт, по кругу (таким образом все прокси используются равномерно)

### Примеры

![Скриншот работы софта](Assets/Screenshot.png)