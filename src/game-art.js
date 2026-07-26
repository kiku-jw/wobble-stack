const CHARACTER_NAMES = ["pear", "cube", "bird", "rabbit", "jelly"];
const CHARACTER_POSES = ["calm", "panic", "impact"];

function getAssetUrl(path) {
  return new URL(`assets/${path}`, document.baseURI).href;
}

function createManifest() {
  const manifest = {
    sky: "route-sky.webp",
    road: "road.webp",
    beam: "beam.webp",
    wheel: "wheel.webp",
    bump: "bump.webp",
    dust: "dust.webp",
    impactStars: "impact-stars.webp",
    cloudLeft: "world/cloud-left.webp",
    cloudMiddle: "world/cloud-middle.webp",
    cloudRight: "world/cloud-right.webp",
    tree: "world/tree.webp",
    mesa: "world/mesa.webp",
    festivalArch: "world/festival-arch.webp",
    windmillTower: "world/windmill-tower.webp",
    windmillRotor: "world/windmill-rotor.webp",
    safeStop: "world/safe-stop.webp",
  };

  for (const name of CHARACTER_NAMES) {
    for (const pose of CHARACTER_POSES) {
      manifest[`${name}-${pose}`] = `characters/${name}-${pose}.webp`;
    }
  }

  return manifest;
}

function loadImage(path) {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.decoding = "async";
    image.addEventListener("load", () => resolve(image), { once: true });
    image.addEventListener("error", () => reject(new Error(`Unable to load ${path}`)), { once: true });
    image.src = getAssetUrl(path);
  });
}

export async function loadGameArt(onProgress = () => {}) {
  const manifest = createManifest();
  const entries = Object.entries(manifest);
  const images = {};
  let loaded = 0;

  await Promise.all(entries.map(async ([key, path]) => {
    images[key] = await loadImage(path);
    loaded += 1;
    onProgress(loaded / entries.length);
  }));

  return images;
}

export function getCharacterArt(images, character, pose) {
  return images[`${character}-${pose}`];
}
