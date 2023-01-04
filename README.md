#  最火热中国开源项目社区之极客玩法

## 使用说明 

需要在 程序目录创建 config.json ， 格式内容如下:

```json
{
  "project": "48907",
  "users": [
    {
      "g_user_id": "7172",
      "oscid": "EupA0S%2BLlRps2qK%2BvOCFLum1%2BCv88Wm0L6LSTjShgc206yB6BHMqwvaw9Vj6DPdFreTNHljBVX%2F%2BbOC5hyTCAo4obVsdTgn91vvWi4sSZRbeTv6pN9afrAUBJScwv9pgQNQLFrKKiy3HyxoeAU9e6NoXVvaWBh95FgxptmOmVZA%3D"
    }
  ]
}
```

1. 打开你的项目页面， 比如 https://www.oschina.net/p/iotsharp 
2. 打开浏览器开发者工具。
3. project 为你的项目ID，控制台输入 page.objId 来获取
4. g_user_id 为用户ID，通过开发者工具的控制台输入 page.g_user_id 来获取。 
5. oscid 为 cookie,  通过开发者工具的应用程序界面cookie中 oscid 获取

官方文档  https://www.oschina.net/question/2918182_2327640


## 发布

 dotnet publish -c Release

