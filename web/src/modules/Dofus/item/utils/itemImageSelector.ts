import type { ItemImage } from '@/modules/Dofus/item/types/item.types';
import { AssetResolution } from '@/modules/Dofus/item/types/assetResolution.enum';

export function getItemImageByResolution(
  images: ItemImage[],
  resolution: AssetResolution,
): ItemImage | null {
  return images.find((img) => img.resolution === resolution) ?? null;
}
