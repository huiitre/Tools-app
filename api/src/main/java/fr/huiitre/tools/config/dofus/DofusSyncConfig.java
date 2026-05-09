package fr.huiitre.tools.config.dofus;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import fr.huiitre.tools.modules.dofus.sync.application.almanax.AlmanaxDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.area.AreaDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.item.ItemDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.itemtype.ItemTypeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.monster.MonsterDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.recipe.RecipeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.subarea.SubAreaDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3AlmanaxDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3AreaDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3ItemDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3ItemTypeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3LanguageDataProviderImpl;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3LocalAssetsReader;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3MonsterDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3RecipeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3.Dofus3SubareaDataProvider;

@Configuration
public class DofusSyncConfig {

    @Bean
    public Dofus3LocalAssetsReader dofus3LocalAssetsReader() {
        return new Dofus3LocalAssetsReader();
    }

    @Bean
    public Dofus3LanguageDataProvider dofus3LanguageDataProvider(
            Dofus3LocalAssetsReader assetsReader) {
        return new Dofus3LanguageDataProviderImpl(assetsReader);
    }

    @Bean
    public ItemTypeDataProvider itemTypeDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        return new Dofus3ItemTypeDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public ItemDataProvider itemDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        return new Dofus3ItemDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public AlmanaxDataProvider almanaxDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        return new Dofus3AlmanaxDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public RecipeDataProvider recipeDataProvider(
            Dofus3LocalAssetsReader assetsReader) {
        return new Dofus3RecipeDataProvider(assetsReader);
    }

    @Bean
    public MonsterDataProvider monsterDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        return new Dofus3MonsterDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public AreaDataProvider areaDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        return new Dofus3AreaDataProvider(assetsReader, languageDataProvider);
    }

    @Bean
    public SubAreaDataProvider subareaDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        return new Dofus3SubareaDataProvider(assetsReader, languageDataProvider);
    }
}
