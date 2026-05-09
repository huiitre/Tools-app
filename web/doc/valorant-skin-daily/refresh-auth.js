import axios from 'axios';
import https from 'https';
import readline from 'readline';
import fs from 'fs';
import dotenv from 'dotenv';

dotenv.config();

const httpsAgent = new https.Agent({ rejectUnauthorized: false });

function ask(question) {
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  return new Promise(resolve => rl.question(question, ans => { rl.close(); resolve(ans.trim()); }));
}

function saveToEnv(key, value) {
  const envPath = './.env';
  let content = fs.existsSync(envPath) ? fs.readFileSync(envPath, 'utf8') : '';
  if (new RegExp(`^${key}=`, 'm').test(content)) {
    content = content.replace(new RegExp(`^${key}=.*`, 'm'), `${key}=${value}`);
  } else {
    content = content.trimEnd() + `\n${key}=${value}\n`;
  }
  fs.writeFileSync(envPath, content);
  process.env[key] = value;
}

function extractPuuid(accessToken) {
  const payload = JSON.parse(Buffer.from(accessToken.split('.')[1], 'base64').toString());
  if (!payload.sub) throw new Error('Token invalide : pas de PUUID');
  return payload.sub;
}

async function refreshToAccess(refreshToken) {
  const params = new URLSearchParams({
    grant_type: 'refresh_token',
    refresh_token: refreshToken,
    client_id: 'prod-xsso-playvalorant',
  });
  const res = await axios.post('https://auth.riotgames.com/token', params.toString(), {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    httpsAgent,
  });
  return { accessToken: res.data.access_token, newRefreshToken: res.data.refresh_token };
}

async function getEntitlement(accessToken) {
  const res = await axios.post(
    'https://entitlements.auth.riotgames.com/api/token/v1',
    {},
    { headers: { Authorization: `Bearer ${accessToken}` }, httpsAgent }
  );
  return res.data.entitlements_token;
}

async function getShop(accessToken, entitlementToken, puuid) {
  const { data: versionData } = await axios.get('https://valorant-api.com/v1/version');
  const clientVersion = versionData.data.riotClientVersion;

  const { data } = await axios.post(
    `https://pd.eu.a.pvp.net/store/v3/storefront/${puuid}`,
    {},
    {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'X-Riot-Entitlements-JWT': entitlementToken,
        'X-Riot-ClientPlatform': 'ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9',
        'X-Riot-ClientVersion': clientVersion,
      },
      httpsAgent,
    }
  );
  return data;
}

const SKIN_TYPE_ID = 'e7c63390-eda7-46e0-bb7a-a6abdacd2433';

async function buildSkinMap() {
  const { data } = await axios.get('https://valorant-api.com/v1/weapons/skins?language=fr-FR');
  const map = {};
  for (const skin of data.data) {
    map[skin.uuid] = skin.displayName;
    for (const level of skin.levels ?? []) map[level.uuid] = skin.displayName;
    for (const chroma of skin.chromas ?? []) map[chroma.uuid] = skin.displayName;
  }
  return map;
}

function resolveSkinNames(skinMap, itemIds) {
  return itemIds.map(id => skinMap[id] ?? `Skin inconnu (${id.slice(0, 8)}...)`);
}

async function getBundles(featuredBundle, skinMap) {
  const bundles = featuredBundle.Bundles ?? (featuredBundle.Bundle ? [featuredBundle.Bundle] : []);
  const VP = '85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741';

  return Promise.all(bundles.map(async (bundle) => {
    const assetId = bundle.DataAssetID;
    let name = `Pack inconnu (${assetId.slice(0, 8)}...)`;
    try {
      const { data } = await axios.get(`https://valorant-api.com/v1/bundles/${assetId}?language=fr-FR`);
      name = data.data.displayName;
    } catch {}

    const baseCost = bundle.TotalBaseCost?.[VP] ?? 0;
    const discountedCost = bundle.TotalDiscountedCost?.[VP] ?? baseCost;
    const discount = bundle.TotalDiscountPercent ?? 0;
    const remaining = bundle.DurationRemainingInSeconds ?? featuredBundle.BundleRemainingDurationInSeconds ?? 0;
    const days = Math.floor(remaining / 86400);
    const hours = Math.floor((remaining % 86400) / 3600);

    const skinItems = (bundle.Items ?? [])
      .filter(i => i.Item?.ItemTypeID === SKIN_TYPE_ID)
      .map(i => ({ id: i.Item.ItemID, cost: i.DiscountedPrice ?? i.BasePrice ?? 0 }));
    const skinIds = skinItems.map(i => i.id);
    const skins = resolveSkinNames(skinMap, skinIds).map((name, idx) => ({ name, id: skinItems[idx].id }));

    return { name, assetId, baseCost, discountedCost, discount, days, hours, skins };
  }));
}

(async () => {
  try {
    let refreshToken = process.env.REFRESH_TOKEN;

    if (refreshToken) {
      console.log('🔑 Refresh token trouvé dans .env');
    } else {
      refreshToken = await ask('Refresh token : ');
      if (!refreshToken) throw new Error('Aucun token fourni');
    }

    console.log('🔄 Échange du refresh token...');
    const { accessToken, newRefreshToken } = await refreshToAccess(refreshToken);
    console.log('✅ Access token obtenu');

    // Toujours sauvegarder le refresh token (nouveau ou initial)
    saveToEnv('REFRESH_TOKEN', newRefreshToken ?? refreshToken);

    const puuid = extractPuuid(accessToken);

    console.log('🔑 Récupération de l\'entitlement...');
    const entitlementToken = await getEntitlement(accessToken);

    console.log('🛒 Récupération du shop...\n');
    const shop = await getShop(accessToken, entitlementToken, puuid);

    const skinMap = await buildSkinMap();

    const offers = shop.SkinsPanelLayout.SingleItemStoreOffers;
    const skinIds = offers.map(o => o.Rewards[0].ItemID);
    const skinNames = resolveSkinNames(skinMap, skinIds);

    console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    console.log('📅 SHOP QUOTIDIEN');
    console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n');

    skinNames.forEach((name, i) => {
      const cost = offers[i].Cost['85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741'];
      console.log(`${i + 1}. ${name} — ${cost} VP`);
      console.log(`   UUID Riot : ${skinIds[i]}`);
    });

    const remaining = shop.SkinsPanelLayout.SingleItemOffersRemainingDurationInSeconds;
    const h = Math.floor(remaining / 3600);
    const m = Math.floor((remaining % 3600) / 60);
    console.log(`\n⏱️  Expire dans ${h}h ${m}min`);

    if (shop.FeaturedBundle) {
      const bundles = await getBundles(shop.FeaturedBundle, skinMap);

      console.log('\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
      console.log('🎁 PACKS EN VENTE');
      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n');

      bundles.forEach(b => {
        const priceStr = b.discount > 0
          ? `${b.discountedCost} VP (au lieu de ${b.baseCost} VP, -${Math.round(b.discount * 100)}%)`
          : `${b.discountedCost} VP`;
        const timeStr = b.days > 0 ? `${b.days}j ${b.hours}h` : `${b.hours}h`;
        console.log(`🎮 ${b.name}`);
        console.log(`   UUID bundle : ${b.assetId}`);
        console.log(`   💰 ${priceStr}`);
        console.log(`   ⏱️  Expire dans ${timeStr}`);
        if (b.skins.length) {
          console.log(`   🔫 Skins inclus :`);
          b.skins.forEach(s => console.log(`      · ${s.name}\n        UUID Riot : ${s.id}`));
        }
        console.log();
      });
    }
  } catch (error) {
    const detail = error.response
      ? `${error.response.status} — ${JSON.stringify(error.response.data)}`
      : error.message;
    console.error('❌', detail);
  }
})();
