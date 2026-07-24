namespace Cirreum.Identity.Provisioning;

/// <summary>
/// The prefix and reserved names for the provisioned custom-claim namespace.
/// </summary>
/// <remarks>
/// Every provisioned claim lives in a <c>custom*</c> namespace on the wire (for example
/// <c>customRoles</c>, <c>customName</c>), so an app-minted claim can never collide with a
/// native or reserved identity-provider claim. The convention is enforced by
/// <see cref="IdentityClaim"/>: its factories are the only construction path and they guarantee
/// the prefix.
/// </remarks>
public static class CustomClaimNames {

	/// <summary>The prefix applied to every provisioned claim name.</summary>
	public const string Prefix = "custom";

	/// <summary>The wire name for provisioned roles.</summary>
	public const string Roles = "customRoles";

	/// <summary>The wire name for a provisioned display name.</summary>
	public const string Name = "customName";

	/// <summary>
	/// Wire-protocol field names, in canonical form, that may never be minted as custom claims.
	/// Building an <see cref="IdentityClaim"/> that resolves to one of these throws.
	/// </summary>
	public static readonly IReadOnlySet<string> Reserved =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "correlationId" };

	/// <summary>
	/// The canonical (native) form of a wire name — the <see cref="Prefix"/> stripped and the
	/// first remaining character lower-cased (<c>customRoles</c> → <c>roles</c>). A name without
	/// the prefix is returned unchanged. This is the same transform the client claims extender
	/// applies to <c>custom*</c> claims.
	/// </summary>
	public static string Canonical(string name) {
		if (name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && name.Length > Prefix.Length) {
			var rest = name[Prefix.Length..];
			return char.ToLowerInvariant(rest[0]) + rest[1..];
		}
		return name;
	}

	/// <summary>
	/// Whether a name resolves to a <see cref="Reserved"/> wire-protocol field — checked against
	/// its <see cref="Canonical(string)"/> form, so a raw name (<c>correlationId</c>) and its
	/// prefixed spelling (<c>customCorrelationId</c>) are treated alike.
	/// </summary>
	public static bool IsReserved(string name) => Reserved.Contains(Canonical(name));

	/// <summary>
	/// The normalized <c>custom*</c> wire name for a claim — the lower-case <see cref="Prefix"/>
	/// followed by the remaining name with its first character upper-cased. Any existing prefix,
	/// in any casing, is replaced, so <c>tenant</c>, <c>customTenant</c>, <c>customtenant</c>, and
	/// <c>CUSTOMtenant</c> all yield the single wire name <c>customTenant</c> — one logical claim,
	/// one wire member.
	/// </summary>
	/// <exception cref="ArgumentException">There is no claim name left once the prefix is removed.</exception>
	public static string ToWireName(string name) {
		var suffix = name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
			? name[Prefix.Length..]
			: name;

		if (string.IsNullOrEmpty(suffix)) {
			throw new ArgumentException($"'{name}' has no claim name beyond the '{Prefix}' prefix.", nameof(name));
		}

		return Prefix + char.ToUpperInvariant(suffix[0]) + suffix[1..];
	}

}
