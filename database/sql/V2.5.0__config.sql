-- table config
ALTER TABLE tools_core.config
  DROP COLUMN scope;

ALTER TABLE tools_core.config
  RENAME COLUMN default_value TO value;