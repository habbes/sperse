## Sample grammar

`statement` -> `expression`

`expression` -> `'remote'` `'('` `expression` `')'`

`expression` -> `assignment` | `addition` | `id` | `constant`

`addition` -> `expression` `'+'` `expression`

`assignment` -> `id` `'='` `expression`
