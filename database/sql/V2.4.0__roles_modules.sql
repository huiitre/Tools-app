-- correction de la clé primaire pour user_module_role
ALTER TABLE tools_core.user_module_role
DROP CONSTRAINT user_module_role_pkey;

ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT user_module_role_pkey
PRIMARY KEY (user_id, module_id);

CREATE INDEX idx_user_module_role_role_id
ON tools_core.user_module_role (role_id);