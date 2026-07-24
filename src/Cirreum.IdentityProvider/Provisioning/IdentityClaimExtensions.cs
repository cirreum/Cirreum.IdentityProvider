namespace Cirreum.Identity.Provisioning;

/// <summary>
/// Projection helpers over a set of <see cref="IdentityClaim"/>, shared by the provider adapters.
/// </summary>
public static class IdentityClaimExtensions {

	/// <summary>
	/// Projects a set of <see cref="IdentityClaim"/> into a canonical map of claim name to values.
	/// Claims sharing a <see cref="IdentityClaim.Type"/> are merged — their values are unioned and
	/// de-duplicated — so a repeated type never fails the projection.
	/// </summary>
	/// <param name="claims">The claims to project.</param>
	/// <returns>A map of <c>custom*</c> claim name to its distinct values.</returns>
	public static IReadOnlyDictionary<string, string[]> ToClaimMap(this IReadOnlyList<IdentityClaim> claims) =>
		claims
			.GroupBy(c => c.Type, StringComparer.Ordinal)
			.ToDictionary(
				g => g.Key,
				g => g.SelectMany(c => c.Values).Distinct(StringComparer.Ordinal).ToArray(),
				StringComparer.Ordinal);

}
