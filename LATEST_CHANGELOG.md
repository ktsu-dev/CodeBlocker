## v2.4.1 (patch)

Changes since v2.4.0:

- Check an expression body's expression before writing its class, and refuse a comment-only one [patch] (#186) (matt-edmondson)
- Drop the blank lines around a braced member body [patch] (#182) (matt-edmondson)
- Brace a member body that holds only a block comment [patch] (#187) (matt-edmondson)
- Skip blank entries in WriteUsings instead of writing `using ;` [patch] (#191) (matt-edmondson)
- Reject an expression-bodied property that also has a default value [patch] (#192) (matt-edmondson)
- Check a template's doc comment before writing the class that holds it [patch] (#193) (matt-edmondson)

