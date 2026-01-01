-- table config
ALTER TABLE config
  DROP COLUMN scope;

ALTER TABLE config
  RENAME COLUMN default_value TO value;