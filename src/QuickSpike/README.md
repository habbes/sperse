## Sample grammar

`statement` -> `expression`

`expression` -> `'remote'` `'('` `expression` `')'` | `'remote'` `'('` `expression` `','` `tag` `')'`

`expression` -> `assignment` | `addition` | `id` | `constant`

`expression` -> `funcCall`

`funcCall` -> `id` `'('` `')'`

`funcCall` -> `id` `'('` `expression` `')'`

`funcCall` -> `id` `'('` `expression` `argList` `')'`

`argList` -> `','` expression `argList`

`argList` -> None

`funcDef` -> `'def'` `id` `'('` `paramList` `')'` `block`

`block` -> `{` exprList `}`

`exprList` -> `\n` `expression` `exprList`

`addition` -> `expression` `'+'` `expression`

`assignment` -> `id` `'='` `expression`
