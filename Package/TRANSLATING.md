# Translating Tarkova-Cola

Thank you for translating the mod! Every text in Tarkova-Cola can be translated with plain JSON files. You don't need to code or recompile anything.

There are two sets of texts, in two folders:

| Folder | What it contains |
|---|---|
| `BepInEx/plugins/TarkovaCola/locales/` | Everything the mod draws itself: notices, the Research / APEX / Classic screens, perk and augment names and descriptions, machine prompts, and the F12 settings. |
| `SPT_Runtime/user/mods/TarkovaCola/locales/` | What the game shows from its own tables: item names and descriptions (bottles, injectors, grenades, case, APEX core...), achievements and quests. |

## How to translate

1. Start the server and the game once. The mod writes up-to-date templates in both folders: `en.json` (English) and `es.json` (Spanish).
   - These templates are regenerated on every start, so don't edit them directly.
2. In each folder, copy `en.json` and rename the copy to your language code. Use the code EFT uses for your language, for example:

   | Code | Language |
   |---|---|
   | `ch` | Chinese |
   | `ru` | Russian |
   | `fr` | French |
   | `de` | German |
   | `jp` | Japanese |
   | `kr` | Korean |
   | `pl` | Polish |
   | `po` | Portuguese |
   | `it` | Italian |
   | `cz` | Czech |
   | `hu` | Hungarian |
   | `sk` | Slovak |
   | `tu` | Turkish |
   | `ro` | Romanian |
   | `es-mx` | Spanish (Mexico) |

3. Translate the **values** only. Leave the keys exactly as they are.
   - Keep placeholders like `{0}` and `{1}`, and line breaks (`\n`), in your translation.
   - In the client file (`BepInEx/plugins/TarkovaCola/locales/`), also set two fields:
     - `"_language"`: the name shown in F12 > Language, for example `"中文"`.
     - `"_culture"`: the same EFT code. With F12 set to **Auto**, the mod picks your file when the game is in that language.
4. Restart the game.
   - The mod uses your file automatically when the game is in that language.
   - You can also choose your language manually in F12 > Language.

Anything you don't translate falls back to English, so a partial translation works fine.

## Fixing English or Spanish

To change an English or Spanish text without losing your change when the templates regenerate, create `en.custom.json` or `es.custom.json` in the same folder. Put only the keys you want to change in it, in the same format as the template.

## Sharing your translation

Send your `<code>.json` files (one from each folder) to the mod author, or open a pull request on GitHub. Translations are bundled in the next release with credit to you.

---

# Traducir Tarkova-Cola

Todos los textos se traducen con archivos JSON, sin tocar código.

1. Arranca el servidor y el juego una vez. Se crean dos plantillas, `en.json` y `es.json`, en dos carpetas:
   - `BepInEx/plugins/TarkovaCola/locales/`: lo que dibuja el mod (pantallas, avisos, perks, mejoras y opciones de F12).
   - `SPT_Runtime/user/mods/TarkovaCola/locales/`: nombres de objetos, logros y misiones.
2. En cada carpeta, copia `en.json` y llama a la copia con el código de idioma de EFT (`ch`, `ru`, `fr`, `de`, `jp`, `kr`, `pl`, `po`, `it`...).
3. Traduce solo los valores. Deja `{0}`, `{1}` y `\n` como están.
4. En el archivo del cliente, pon también `_language` (el nombre que sale en F12) y `_culture` (el código).
5. Reinicia el juego.

Lo que no traduzcas sale en inglés. Para corregir el inglés o el español sin que se borre al regenerar las plantillas, usa `en.custom.json` o `es.custom.json`.
