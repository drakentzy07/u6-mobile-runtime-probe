import { AnimationClip, Bone, Object3D, QuaternionKeyframeTrack, VectorKeyframeTrack } from 'three';
import { describe, expect, it } from 'vitest';
import { retargetQ1Clip } from '../src/highfly/character_q1_clip_retarget';

function rig(name: string, position: [number, number, number]) {
  const scene = new Object3D();
  const bone = new Bone();
  bone.name = name;
  bone.position.set(...position);
  scene.add(bone);
  return { scene, bone };
}

function expectVector(actual: ArrayLike<number>, expected: number[]) {
  expect(actual.length).toBe(expected.length);
  expected.forEach((value, i) => {
    expect(actual[i]).toBeCloseTo(value, 6);
  });
}

describe('Q1 external class animation retargeting', () => {
  it('moves the donor rest baseline to Q1 while preserving motion, rotation and timing', () => {
    const donor = rig('hips', [0.2, 0.4, -0.1]);
    const target = rig('hips', [0.3, 0.95, 0.05]);
    const position = new VectorKeyframeTrack(
      'hips.position',
      [0, 0.5, 1.5],
      [0.2, 0.4, -0.1, 0.25, 0.47, -0.08, 0.15, 0.37, -0.12],
    );
    const rotation = new QuaternionKeyframeTrack(
      'hips.quaternion',
      [0, 0.5, 1.5],
      [0, 0, 0, 1, 0, Math.SQRT1_2, 0, Math.SQRT1_2, 0, 0, 0, 1],
    );
    const source = new AnimationClip('Running_A', 1.5, [position, rotation]);
    const originalPosition = Array.from(position.values);
    const originalRotation = Array.from(rotation.values);
    const result = retargetQ1Clip(source, donor.scene, target.scene);

    expectVector(result.tracks[0].values, [0.3, 0.95, 0.05, 0.35, 1.02, 0.07, 0.25, 0.92, 0.03]);
    // The gait's displacements and timing remain the donor's exact motion.
    const moved = result.tracks[0].values;
    expect(moved[4] - moved[1]).toBeCloseTo(position.values[4] - position.values[1], 6);
    expect(moved[6] - moved[0]).toBeCloseTo(position.values[6] - position.values[0], 6);
    expect(Array.from(result.tracks[1].values)).toEqual(originalRotation);
    expect(result.tracks.map((t) => Array.from(t.times))).toEqual(
      source.tracks.map((t) => Array.from(t.times)),
    );
    expect(result.name).toBe(source.name);
    expect(result.duration).toBe(source.duration);
    expect(result.blendMode).toBe(source.blendMode);
    expect(Array.from(position.values)).toEqual(originalPosition);
    expect(Array.from(rotation.values)).toEqual(originalRotation);
    expect(donor.bone.position.toArray()).toEqual([0.2, 0.4, -0.1]);
    expect(target.bone.position.toArray()).toEqual([0.3, 0.95, 0.05]);
  });

  it('leaves canonical CLAUDE and Q1 native clips untouched when their scene is unchanged', () => {
    const canonical = rig('hips', [0, 0.4, 0]);
    const source = new AnimationClip('Idle', 1, [
      new VectorKeyframeTrack('hips.position', [0, 1], [0, 0.4, 0, 0, 0.42, 0]),
    ]);
    const before = Array.from(source.tracks[0].values);
    expect(retargetQ1Clip(source, canonical.scene, canonical.scene)).toBe(source);
    expect(Array.from(source.tracks[0].values)).toEqual(before);
  });

  it('prevents a second baseline shift on a clip already adapted for this Q1 scene', () => {
    const donor = rig('hips', [0, 0.4, 0]);
    const target = rig('hips', [0, 0.95, 0]);
    const source = new AnimationClip('Idle', 1, [
      new VectorKeyframeTrack('hips.position', [0, 1], [0, 0.4, 0, 0, 0.42, 0]),
    ]);
    const adapted = retargetQ1Clip(source, donor.scene, target.scene);
    const before = Array.from(adapted.tracks[0].values);
    expect(retargetQ1Clip(adapted, donor.scene, target.scene)).toBe(adapted);
    expect(Array.from(adapted.tracks[0].values)).toEqual(before);
    expectVector(adapted.tracks[0].values, [0, 0.95, 0, 0, 0.97, 0]);
  });

  it('resolves GLTFLoader sanitized bone names without shifting unrelated tracks', () => {
    const donor = rig('wrist.r', [0, 0.26, 0]);
    const target = rig('wrist.r', [0, 0.24, 0]);
    const source = new AnimationClip('Attack', 1, [
      new VectorKeyframeTrack('wristr.position', [0, 1], [0, 0.26, 0, 0, 0.27, 0]),
      new VectorKeyframeTrack('unrelated.position', [0, 1], [1, 2, 3, 4, 5, 6]),
    ]);
    const result = retargetQ1Clip(source, donor.scene, target.scene);
    expectVector(result.tracks[0].values, [0, 0.24, 0, 0, 0.25, 0]);
    expect(Array.from(result.tracks[1].values)).toEqual([1, 2, 3, 4, 5, 6]);
  });

  it('preserves cubic-spline tangents and shifts only the position values', () => {
    const donor = rig('hips', [0, 0.4, 0]);
    const target = rig('hips', [0, 0.95, 0]);
    const values = [1, 2, 3, 0, 0.4, 0, 4, 5, 6, 7, 8, 9, 0, 0.45, 0, 10, 11, 12];
    const position = new VectorKeyframeTrack('hips.position', [0, 1], values);
    expect(position.getValueSize()).toBe(9);
    const source = new AnimationClip('SplineCast', 1, [position]);
    const result = retargetQ1Clip(source, donor.scene, target.scene);
    expectVector(
      result.tracks[0].values,
      [1, 2, 3, 0, 0.95, 0, 4, 5, 6, 7, 8, 9, 0, 1, 0, 10, 11, 12],
    );
    expectVector(source.tracks[0].values, values);
    expect(result.tracks[0].getInterpolation()).toBe(position.getInterpolation());
  });
});
