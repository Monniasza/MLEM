﻿using System;

namespace MLEM.Misc {
    /// <summary>
    /// A seed source contains an <see cref="int"/> value which can be used as a seed for a <see cref="System.Random"/> or <see cref="SingleRandom"/>. Seed sources feature a convenient way to add multiple seeds using <see cref="Add(int)"/>, which will be sufficiently scrambled to be deterministically pseudorandom and combined into a single <see cref="int"/>.
    /// This struct behaves similarly to <c>System.HashCode</c> in many ways, with an important distinction being that <see cref="SeedSource"/>'s scrambling procedure is not considered an implementation detail, and will stay consistent between process executions.
    /// </summary>
    /// <example>
    /// For example, a seed source can be used to create a new <see cref="System.Random"/> based on an object's <c>x</c> and <c>y</c> coordinates by combining them into a <see cref="SeedSource"/> using <see cref="Add(int)"/>. The values generated by the <see cref="System.Random"/> created using <see cref="Random()"/> will then be determined by the specific pair of <c>x</c> and <c>y</c> values used.
    /// </example>
    public readonly struct SeedSource {

        private readonly int? value;

        /// <summary>
        /// Creates a new seed source from the given seed, which will be added automatically using <see cref="Add(int)"/>.
        /// </summary>
        /// <param name="seed">The initial seed to use.</param>
        public SeedSource(int seed) : this() {
            this = this.Add(seed);
        }

        /// <summary>
        /// Creates a new seed source from the given set of seeds, which will be added automatically using <see cref="Add(int)"/>.
        /// </summary>
        /// <param name="seeds">The initial seeds to use.</param>
        public SeedSource(params int[] seeds) : this() {
            foreach (var seed in seeds)
                this = this.Add(seed);
        }

        private SeedSource(int? value) {
            this.value = value;
        }

        /// <summary>
        /// Adds the given seed to this seed source's value and returns the result as a new seed source.
        /// The algorithm used for adding involves various scrambling operations that sufficiently pseudo-randomize the seed and final value.
        /// </summary>
        /// <param name="seed">The seed to add.</param>
        /// <returns>A new seed source with the seed added.</returns>
        public SeedSource Add(int seed) {
            return new SeedSource(new int?(SeedSource.Scramble(this.Get()) + SeedSource.Scramble(seed)));
        }

        /// <summary>
        /// Adds the given seed to this seed source's value and returns the result as a new seed source.
        /// Floating point values are scrambled by invoking <see cref="Add(int)"/> using a typecast version, followed by invoking <see cref="Add(int)"/> using the decimal value multiplied by <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="seed">The seed to add.</param>
        /// <returns>A new seed source with the seed added.</returns>
        public SeedSource Add(float seed) {
            return this.Add((int) seed).Add((seed - (int) seed) * int.MaxValue);
        }

        /// <summary>
        /// Adds the given seed to this seed source's value and returns the result as a new seed source.
        /// Strings are scrambled by invoking <see cref="Add(int)"/> using every character contained in the string, in order.
        /// </summary>
        /// <param name="seed">The seed to add.</param>
        /// <returns>A new seed source with the seed added.</returns>
        public SeedSource Add(string seed) {
            var ret = this;
            foreach (var c in seed)
                ret = ret.Add(c);
            return ret;
        }

        /// <summary>
        /// Returns a new seed source whose value is this seed source's value, but scrambled further.
        /// In essence, this creates a new seed source whose value is determined by the current seed source.
        /// </summary>
        /// <returns>A new seed source with a rotated value.</returns>
        public SeedSource Rotate() {
            return new SeedSource(new int?(SeedSource.Scramble(this.Get())));
        }

        /// <summary>
        /// Returns this seed source's seed value, which can then be used in <see cref="SingleRandom"/> or elsewhere.
        /// </summary>
        /// <returns>This seed source's value.</returns>
        public int Get() {
            return this.value ?? 1623487;
        }

        /// <summary>
        /// Returns a new <see cref="Random"/> instance using this source seed's value, retrieved using <see cref="Get"/>.
        /// </summary>
        /// <returns>A new <see cref="Random"/> using this seed source's value.</returns>
        public Random Random() {
            return new Random(this.Get());
        }

        private static int Scramble(int x) {
            x += 84317;
            x ^= x << 7;
            x *= 207398809;
            x ^= x << 17;
            x *= 928511849;
            return x;
        }

    }
}
