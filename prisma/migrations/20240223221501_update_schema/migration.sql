/*
  Warnings:

  - You are about to drop the column `label_index` on the `Label` table. All the data in the column will be lost.
  - Added the required column `index` to the `Label` table without a default value. This is not possible if the table is not empty.

*/
-- RedefineTables
PRAGMA foreign_keys=OFF;
CREATE TABLE "new_Label" (
    "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "index" INTEGER NOT NULL,
    "label_id" INTEGER NOT NULL,
    "repository" TEXT NOT NULL
);
INSERT INTO "new_Label" ("id", "label_id", "repository") SELECT "id", "label_id", "repository" FROM "Label";
DROP TABLE "Label";
ALTER TABLE "new_Label" RENAME TO "Label";
PRAGMA foreign_key_check;
PRAGMA foreign_keys=ON;
