/*
	This script is placed in public domain. The author takes no responsibility for any possible harm.
	Contributed by Jonathan Czeck
*/
using UnityEngine;
using System.Collections;

public class LightningBolt : MonoBehaviour
{
	public Transform target;
	public int zigs = 100;
	public float speed = 1f;
	public float scale = 1f;
	public Light startLight;
	public Light endLight;
	
	Perlin noise;
	float oneOverZigs;
	
	private ParticleSystem particleSystemComponent;
	private ParticleSystem.Particle[] particles;
	
	void Start()
	{
		oneOverZigs = 1f / (float)zigs;
		particleSystemComponent = GetComponent<ParticleSystem>();
		if (particleSystemComponent == null)
		{
			particleSystemComponent = gameObject.AddComponent<ParticleSystem>();
		}

		var emission = particleSystemComponent.emission;
		emission.enabled = false;

		var main = particleSystemComponent.main;
		main.playOnAwake = false;
		main.loop = false;
		main.maxParticles = Mathf.Max(zigs, 2);
		main.startLifetime = 1f;
		main.startSpeed = 0f;
		main.startSize = 0.1f;

		particleSystemComponent.Clear();
		particleSystemComponent.Emit(zigs);
		particles = new ParticleSystem.Particle[Mathf.Max(zigs, 2)];
	}
	
	void Update ()
	{
		if (target == null || particleSystemComponent == null)
			return;

		if (noise == null)
			noise = new Perlin();

		if (particles == null || particles.Length < zigs)
			particles = new ParticleSystem.Particle[Mathf.Max(zigs, 2)];

		int count = particleSystemComponent.GetParticles(particles);
		if (count < zigs)
		{
			particleSystemComponent.Emit(zigs - count);
			count = particleSystemComponent.GetParticles(particles);
		}
			
		float timex = Time.time * speed * 0.1365143f;
		float timey = Time.time * speed * 1.21688f;
		float timez = Time.time * speed * 2.5564f;
		
		for (int i=0; i < Mathf.Min(zigs, count); i++)
		{
			Vector3 position = Vector3.Lerp(transform.position, target.position, oneOverZigs * (float)i);
			Vector3 offset = new Vector3(noise.Noise(timex + position.x, timex + position.y, timex + position.z),
										noise.Noise(timey + position.x, timey + position.y, timey + position.z),
										noise.Noise(timez + position.x, timez + position.y, timez + position.z));
			position += (offset * scale * ((float)i * oneOverZigs));
			
			particles[i].position = position;
			particles[i].startColor = Color.white;
			particles[i].remainingLifetime = 1f;
			particles[i].startLifetime = 1f;
		}
		
		particleSystemComponent.SetParticles(particles, count);
		
		if (count >= 2)
		{
			if (startLight)
				startLight.transform.position = particles[0].position;
			if (endLight && count > 0)
				endLight.transform.position = particles[Mathf.Min(count, zigs) - 1].position;
		}
	}	
}
