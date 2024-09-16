using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Material references a Shader and holds the applied values for each Uniform and Texture Sampler.
/// This way, you can have a single Shader in memory but many different applied values.
/// </summary>
public class Material
{
	public readonly record struct BoundSampler(Texture? Texture, TextureSampler Sampler);

	/// <summary>
	/// Vertex Texture Samplers bound to this Material
	/// </summary>
	public readonly BoundSampler[] VertexSamplers = new BoundSampler[16];

	/// <summary>
	/// Fragment Texture Samplers bound to this Material
	/// </summary>
	public readonly BoundSampler[] FragmentSamplers = new BoundSampler[16];

	/// <summary>
	/// The current Shader the Material is using.
	/// If null, the Material will not have any Uniforms.
	/// </summary>
	public Shader? Shader
	{
		get => shader;
		set => SetShader(value);
	}

	/// <summary>
	/// Constructs an Empty Material
	/// </summary>
	public Material() { }

	/// <summary>
	/// Constructs a Material using the given Shader
	/// </summary>
	public Material(Shader? shader) => SetShader(shader);

	private Shader? shader;

	internal byte[] VertexUniformBuffer = [];
	internal byte[] FragmentUniformBuffer = [];

	public void SetShader(Shader? shader)
	{
		if (this.shader == shader)
			return;
		this.shader = shader;
		if (shader == null)
			return;

		if (VertexUniformBuffer.Length < shader.Vertex.UniformSizeInBytes)
			Array.Resize(ref VertexUniformBuffer, shader.Vertex.UniformSizeInBytes);
		if (FragmentUniformBuffer.Length < shader.Fragment.UniformSizeInBytes)
			Array.Resize(ref FragmentUniformBuffer, shader.Fragment.UniformSizeInBytes);
	}

	public void Clear()
	{
		SetShader(null);
		Array.Fill(VertexSamplers, default);
		Array.Fill(FragmentSamplers, default);
	}

	public void CopyTo(Material other)
	{
		other.SetShader(shader);
		Array.Copy(VertexSamplers, other.VertexSamplers, VertexSamplers.Length);
		Array.Copy(FragmentSamplers, other.FragmentSamplers, FragmentSamplers.Length);
		Array.Copy(VertexUniformBuffer, other.VertexUniformBuffer, Math.Min(VertexUniformBuffer.Length, other.VertexUniformBuffer.Length));
		Array.Copy(FragmentUniformBuffer, other.FragmentUniformBuffer, Math.Min(FragmentUniformBuffer.Length, other.FragmentUniformBuffer.Length));
	}

	public bool Has(string uniform)
		=> Has(uniform, out _, out _);

	public bool Has(string uniform, out UniformType type, out int arrayElements)
	{
		type = UniformType.None;
		arrayElements = 0;

		if (shader == null)
			return false;

		foreach (var it in shader.Vertex.Uniforms)
			if (it.Name.Equals(uniform))
			{
				type = it.Type;
				arrayElements = it.ArrayElements;
				return true;
			}

		foreach (var it in shader.Fragment.Uniforms)
			if (it.Name.Equals(uniform))
			{
				type = it.Type;
				arrayElements = it.ArrayElements;
				return true;
			}
			
		return false;
	}

	public void Set(string uniform, float value)
		=> Set(uniform, stackalloc float[1] { value });

	public void Set(string uniform, Vector2 value)
		=> Set(uniform, stackalloc float[2] { value.X, value.Y });

	public void Set(string uniform, Vector3 value)
		=> Set(uniform, stackalloc float[3] { value.X, value.Y, value.Z });

	public void Set(string uniform, Vector4 value)
		=> Set(uniform, stackalloc float[4] { value.X, value.Y, value.Z, value.W });

	public void Set(string uniform, Matrix3x2 value)
		=> Set(uniform, stackalloc float[6] { value.M11, value.M12, value.M21, value.M22, value.M31, value.M32 });

	public void Set(string uniform, Matrix4x4 value)
		=> Set(uniform, stackalloc float[16] { 
			value.M11, value.M12, value.M13, value.M14, 
			value.M21, value.M22, value.M23, value.M24,
			value.M31, value.M32, value.M33, value.M34,
			value.M41, value.M42, value.M43, value.M44,
		});

	public void Set(string uniform, Color value)
		=> Set(uniform, value.ToVector4());

	public unsafe void Set(string uniform, ReadOnlySpan<Vector2> value)
	{
		fixed (Vector2* ptr = value)
			Set(uniform, new ReadOnlySpan<float>((float*)ptr, value.Length * 2));
	}

	public unsafe void Set(string uniform, ReadOnlySpan<Vector3> value)
	{
		fixed (Vector3* ptr = value)
			Set(uniform, new ReadOnlySpan<float>((float*)ptr, value.Length * 3));
	}

	public unsafe void Set(string uniform, ReadOnlySpan<Vector4> value)
	{
		fixed (Vector4* ptr = value)
			Set(uniform, new ReadOnlySpan<float>((float*)ptr, value.Length * 4));
	}
	
	public void Set(string uniform, ReadOnlySpan<Color> value)
	{
		Span<float> data = stackalloc float[value.Length * 4];
		for (int i = 0, n = 0; i < value.Length; i ++, n += 4)
		{
			var vec4 = value[i].ToVector4();
			data[n + 0] = vec4.X;
			data[n + 1] = vec4.Y;
			data[n + 2] = vec4.Z;
			data[n + 3] = vec4.W;
		}
		Set(uniform, data);
	}
	
	public void Set(string uniform, ReadOnlySpan<Matrix4x4> value)
	{
		Span<float> data = stackalloc float[value.Length * 16];
		for (int i = 0, n = 0; i < value.Length; i ++, n += 16)
		{
			data[n + 0] = value[i].M11; data[n + 1] = value[i].M12; data[n + 2] = value[i].M13; data[n + 3] = value[i].M14; 
			data[n + 4] = value[i].M21; data[n + 5] = value[i].M22; data[n + 6] = value[i].M23; data[n + 7] = value[i].M24;
			data[n + 8] = value[i].M31; data[n + 9] = value[i].M32; data[n +10] = value[i].M33; data[n +11] = value[i].M34;
			data[n +12] = value[i].M41; data[n +13] = value[i].M42; data[n +14] = value[i].M43; data[n +15] = value[i].M44;
		}
		Set(uniform, data);
	}

	public unsafe void Set(string uniform, ReadOnlySpan<float> values)
	{
		fixed (void* ptr = values)
			Set(uniform, new ReadOnlySpan<byte>(ptr, values.Length * sizeof(float)));
	}

	public void Set(string uniform, ReadOnlySpan<byte> data)
	{
		static unsafe void FindUniformAndCopyData(string name, ShaderUniform[] uniforms, in ReadOnlySpan<byte> srcBuffer, Span<byte> dstBuffer)
		{
			var src = srcBuffer;
			var offset = 0;
			foreach (var it in uniforms)
			{
				if (it.Name == name)
				{
					var dst = dstBuffer[offset..];
					if (src.Length > dst.Length)
						src = src[0..dst.Length];
					src.CopyTo(dst);
					break;
				}
				offset += it.Type.SizeInBytes() * it.ArrayElements;
			}
		}

		if (shader != null)
		{
			FindUniformAndCopyData(uniform, shader.Vertex.Uniforms, data, VertexUniformBuffer);
			FindUniformAndCopyData(uniform, shader.Fragment.Uniforms, data, FragmentUniformBuffer);
		}
	}

}
