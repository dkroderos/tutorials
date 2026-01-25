BEGIN;

DELETE FROM todos t1
USING todos t2
WHERE t1.name = t2.name
  AND t1.created_at > t2.created_at;

ALTER TABLE todos
ADD CONSTRAINT todos_name_unique UNIQUE (name);

COMMIT;
