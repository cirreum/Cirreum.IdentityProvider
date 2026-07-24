namespace Cirreum.Identity.Provisioning;

/// <summary>
/// One constituent of the identity an application provisions for a user — a single claim to be
/// minted into the issued token, in the <c>custom*</c> wire namespace.
/// </summary>
/// <remarks>
/// <para>
/// Construct via the factories — <see cref="Roles(string[])"/>, <see cref="Name(string)"/>, or
/// <see cref="Of(string, string[])"/>. The constructor is private, so every
/// <see cref="IdentityClaim"/> is guaranteed to carry a <c>custom*</c> wire name and never a
/// reserved wire-protocol name.
/// </para>
/// <para>
/// <see cref="Values"/> is always a list; a single-valued claim is a one-element list. Whether a
/// value is emitted on the wire as a scalar or an array is the provider adapter's concern. The
/// <c>custom*</c> prefix is a wire/collision-avoidance detail (see <see cref="CustomClaimNames"/>);
/// the client canonicalizes it back to the native claim.
/// </para>
/// </remarks>
public sealed record IdentityClaim {

	/// <summary>The <c>custom*</c> wire name of the claim.</summary>
	public string Type { get; }

	/// <summary>The claim's value(s).</summary>
	public IReadOnlyList<string> Values { get; }

	// Private: the factories are the only construction path, so the custom* convention is
	// type-enforced — a non-custom* or reserved name is unconstructable, not merely guarded.
	private IdentityClaim(string type, IReadOnlyList<string> values) {
		if (CustomClaimNames.IsReserved(type)) {
			throw new ArgumentException(
				$"'{type}' resolves to the reserved protocol field '{CustomClaimNames.Canonical(type)}' and cannot be minted as an identity claim.",
				nameof(type));
		}

		if (values is null || values.Count == 0) {
			throw new ArgumentException($"Identity claim '{type}' must carry at least one value.", nameof(values));
		}

		// Defensive copy so the claim is immutable regardless of what the caller passes/mutates.
		var copy = new string[values.Count];
		for (var i = 0; i < values.Count; i++) {
			if (string.IsNullOrWhiteSpace(values[i])) {
				throw new ArgumentException($"Identity claim '{type}' has a null or blank value.", nameof(values));
			}
			copy[i] = values[i];
		}

		this.Type = type;
		this.Values = copy;
	}

	/// <summary>Roles to mint, under the reserved <see cref="CustomClaimNames.Roles"/> wire name.</summary>
	public static IdentityClaim Roles(params string[] roles) => new(CustomClaimNames.Roles, roles);

	/// <summary>Roles to mint, under the reserved <see cref="CustomClaimNames.Roles"/> wire name.</summary>
	public static IdentityClaim Roles(IReadOnlyList<string> roles) => new(CustomClaimNames.Roles, roles);

	/// <summary>A display name to mint, under the reserved <see cref="CustomClaimNames.Name"/> wire name.</summary>
	public static IdentityClaim Name(string name) => new(CustomClaimNames.Name, [name]);

	/// <summary>
	/// An arbitrary application claim. The name is normalized into the <c>custom*</c> wire namespace
	/// (see <see cref="CustomClaimNames.ToWireName(string)"/>): any existing <c>custom</c> prefix, in
	/// any casing, is replaced with the canonical form, so <c>Of("tenant")</c>, <c>Of("customTenant")</c>,
	/// and <c>Of("CUSTOMtenant")</c> all yield the single wire name <c>customTenant</c>.
	/// </summary>
	/// <param name="name">The claim name, with or without the <c>custom</c> prefix. Must be non-blank and have a name beyond the prefix.</param>
	/// <param name="values">One or more non-blank values.</param>
	/// <exception cref="ArgumentException">The name is blank, resolves to a reserved wire-protocol field, or has no name beyond the prefix; or the values are empty or contain a blank.</exception>
	public static IdentityClaim Of(string name, params string[] values) {
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if (CustomClaimNames.IsReserved(name)) {
			throw new ArgumentException(
				$"'{name}' resolves to the reserved protocol field '{CustomClaimNames.Canonical(name)}' and cannot be minted.",
				nameof(name));
		}

		return new(CustomClaimNames.ToWireName(name), values);
	}

}
