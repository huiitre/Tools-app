ALTER TABLE tools_core.users
ADD COLUMN avatar_source VARCHAR(32) NOT null default 'PASSWORD';
COMMENT ON COLUMN tools_core.users.avatar_source IS
'Source de l’avatar utilisateur (PASSWORD, GOOGLE, GITHUB, GRAVATAR, UPLOADED, etc.)';

ALTER TABLE tools_core.user_auth_provider
ADD COLUMN provider_avatar_url VARCHAR(512) default NULL;
COMMENT ON COLUMN tools_core.user_auth_provider.provider_avatar_url IS
'Url de l''avatar fourni par le provider (Google, GitHub, etc.)';

-- update des codes de chaque module actuel
UPDATE tools_core.module
SET code = CASE code
  WHEN 'TOOLS_HEALTH'   THEN 'health'
  WHEN 'TOOLS_TODOLIST' THEN 'todolist'
  WHEN 'TOOLS_DOFUS'    THEN 'dofus'
END
WHERE code IN (
  'TOOLS_HEALTH',
  'TOOLS_TODOLIST',
  'TOOLS_DOFUS'
);