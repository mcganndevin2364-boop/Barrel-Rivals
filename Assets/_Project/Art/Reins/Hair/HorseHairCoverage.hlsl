#ifndef BARREL_RIVALS_HAIR_COVERAGE_INCLUDED
#define BARREL_RIVALS_HAIR_COVERAGE_INCLUDED
// The dense foundation is continuous at the crest and reveals the source mask
// toward its tips. All outer locks, forelock, tail and rider hair retain their atlas.
half HorseHairCoverage(half atlasAlpha,half progress,half foundation,half strength)
{
    half baseCoverage=saturate(foundation*strength)*(1-smoothstep(.32h,.84h,progress));
    return max(atlasAlpha,baseCoverage);
}
#endif
