/*
  Warnings:

  - You are about to alter the column `repository` on the `Label` table. The data in that column could be lost. The data in that column will be cast from `String` to `Int`.

*/
-- RedefineTables
PRAGMA foreign_keys=OFF;
CREATE TABLE "new_Label" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "index" INTEGER NOT NULL,
    "label" INTEGER NOT NULL,
    "repository" INTEGER NOT NULL
);
INSERT INTO "new_Label" ("id", "index", "label", "repository") SELECT "id", "index", "label", "repository" FROM "Label";
DROP TABLE "Label";
ALTER TABLE "new_Label" RENAME TO "Label";
PRAGMA foreign_key_check;
PRAGMA foreign_keys=ON;
