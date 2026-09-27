# DecollateFR

Patch communautaire français pour [**Decollate Decoration**](https://store.steampowered.com/app/3155570/Decollate_Decoration/), chargé par BepInEx 5 et Harmony sans modifier `Assembly-CSharp.dll`.

## Méthodes hookées

- `Game.readText(string)` : couvre les lectures directes comme les endings avec `localize/en/...`.
- `Game.LoadText(string, string, bool)` : couvre `Game.LoadScriptFile`, les définitions, l'aide, les licences et les documents. Cette méthode privée contourne `Game.readText`, donc ce second hook est nécessaire.
- `LocalizeFile.readFrom(string, bool)` : applique des remplacements français clairsemés aux tables à colonnes, uniquement dans la colonne interne `Language.EN`.
- `GameInitialize.Lang` et `LanguageSetting.LocalizeFolder` : maintiennent respectivement `Language.EN` et `localize/en` pendant que le patch est actif.

Les accès directs de `Shogi` à ses fichiers de plateau restent intacts : ce sont des données de jeu et non des textes à traduire.

## 1. Installer BepInEx

Le jeu est Mono x64. Téléchargez [BepInEx 5.4.23.5 x64](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip), puis extrayez le contenu de l'archive dans le dossier contenant `Decollate Decoration.exe`.

Lancez le jeu une fois. BepInEx doit créer `BepInEx/config`, `BepInEx/plugins` et le journal `BepInEx/LogOutput.log`.

## 2. Construire

Prérequis : SDK .NET et une installation BepInEx 5 extraite.

```powershell
.\Build.ps1 `
  -GameDir 'D:\SteamLibrary\steamapps\common\Decollate Decoration' `
  -BepInExDir 'C:\chemin\vers\BepInEx-5.4.23.5'
```

Si BepInEx est déjà installé dans le dossier du jeu, omettez `-BepInExDir`.

Le projet référence les DLL du jeu et de BepInEx sans les copier dans le résultat. L'archive distribuable est créée dans `dist/DecollateFR.zip` et ne contient aucun fichier propriétaire du jeu.

## 3. Installer le patch

Extrayez `DecollateFR.zip` à la racine du jeu. La structure obtenue doit être :

```text
BepInEx/
└── plugins/
    └── DecollateFR/
        ├── DecollateFR.dll
        └── translations/
```

## 4. Ajouter les traductions

Les fichiers sont lus strictement en UTF-8. Les fins de ligne sont normalisées en CRLF pour rester compatibles avec `EndingScene`.

Pour les scripts, définitions et documents chargés par `Game.LoadText`, reproduisez le chemin situé après `localize/en/` :

```text
translations/def/system_text.txt
translations/script/scenario0_1_1.txt
translations/help/help_steam.txt
```

Un appel direct à `Game.readText("localize/en/example")` cherche successivement :

```text
translations/example
translations/example.txt
```

Les tables chargées par `LocalizeFile` utilisent un format clairsemé qui évite de recopier les colonnes propriétaires :

```text
# translations/tables/example.tsv
KEY_ONE<TAB>Texte français
KEY_TWO<TAB>Autre texte français
```

Seules les clés présentes dans la table originale sont remplacées, dans la colonne `Language.EN`. Les autres valeurs restent en anglais.

## 5. Règles de traduction VN

- Ne modifiez pas les commandes, labels, placeholders, balises `%...`, `@n`, `@s` ou séquences de contrôle.
- Conservez les tabulations lorsque le format les utilise.
- Ne placez pas `//` dans un texte chargé avec suppression des commentaires : le jeu considère tout ce qui suit comme un commentaire.
- Ne distribuez que `DecollateFR.dll` et vos traductions originales.

## 6. Tester

1. Placez une traduction minimale dans `translations/def/system_text.txt` ou un script complet dans `translations/script/`.
2. Lancez le jeu depuis Steam.
3. Vérifiez dans `BepInEx/LogOutput.log` les lignes `DecollateFR ... chargé` et `Traduction chargée`.
4. Vérifiez le menu, une scène VN, un ending, les sauvegardes et les changements de langue dans les options.
5. Retirez temporairement un fichier FR : le contenu anglais original doit reprendre sans erreur.

Le build prouve que les hooks correspondent aux signatures de l'assembly installé. La validation des glyphes français et du comportement visuel demande encore un test réel avec les traductions finales.
