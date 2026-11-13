## v2.4.3 (patch)

Changes since v2.4.2:

- Write no namespace line for a whitespace-only namespace [patch] (#204) (matt-edmondson)
- Check a type once per write, so a nested expression body runs twice, not depth + 3 times [patch] (#205) (matt-edmondson)
- Write a null quoted DefaultValue as "" instead of throwing [patch] (#210) (matt-edmondson)
- Skip blank doc crefs and refuse null doc tags before writing [patch] (#211) (matt-edmondson)

