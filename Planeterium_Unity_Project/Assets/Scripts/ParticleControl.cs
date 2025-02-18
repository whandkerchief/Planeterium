using UnityEngine;
using System.Collections;

/*
This script defines the boundaries for a star where particles can travel.
Particles are emitted from a central point and emmitted in a circle. If the particle
travels outside of the boundary defined by the boundary texture, the lifetime is set to 0. 
This script also defines the colour of the star based on the input number. 
*/
public class ParticleControl : MonoBehaviour
{
    [Header("Star control properties")]
    public Texture2D boundaryTexture;  // Defines allowed bounds
    [SerializeField] private float textureWidth = 1;     // Width of texture in world space
    [SerializeField] private float textureHeight = 1;    // Height of texture in world space
    [SerializeField] private int checkInterval = 5;      // Run boundary check every 5 frames

    [SerializeField] private ParticleSystem particleSystem;     //The particle system of the star
    private ParticleSystem.Particle[] particles;
    private bool[,] boundaryLookup;
    private int frameCounter = 0;

    //Initialize star by setting a preset boundary 
    void Start()
    {
        SetBoundary();
    }

    //Run a boundary check for the star every check interval. Skips most frames for efficiency
    void LateUpdate()
    {
        if (++frameCounter % checkInterval != 0) return; // Skip processing most frames

        //Get the current set of particles in the system
        if (particles == null || particles.Length < particleSystem.main.maxParticles)
        {
            particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        }

        int numParticlesAlive = particleSystem.GetParticles(particles);

        //Run checks to see if the particle is within bounds
        for (int i = 0; i < numParticlesAlive; i++)
        {
            Vector3 particlePosition = particles[i].position;

            // Map world position to array indices
            int x = Mathf.Clamp(Mathf.RoundToInt((particlePosition.x + textureWidth / 2f) / textureWidth * boundaryTexture.width), 0, boundaryTexture.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt((particlePosition.y + textureHeight / 2f) / textureHeight * boundaryTexture.height), 0, boundaryTexture.height - 1);

            // Check precomputed boundary array
            if (boundaryLookup[x, y])
            {
                particles[i].remainingLifetime = 0f;
            }
        }

        particleSystem.SetParticles(particles, numParticlesAlive);
    }

    //Change the colour gradient that will be applied to the particle's colour over lifetime module
    public void ChangeColour(float hue)
    {
        var colorModule = particleSystem.colorOverLifetime;
        colorModule.enabled = true;

        Color firstColor = Color.HSVToRGB(hue, 1f, 1f);  //The colour at the center
        Color secondColor = Color.HSVToRGB(0f, 0f, 0f);  //Black colour that the particle will fade to

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(firstColor, 0.0f),   //0% - first color
                new GradientColorKey(secondColor, 0.14f),   //14% - black
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),  // Fully visible at 0%
                new GradientAlphaKey(1.0f, 1.0f)   // Fully visible at 100%
            }
        );

        // Apply the gradient
        colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    //Murmur hash function. Same as the one in the blob generator, used to get a hue value to define star colour. 
    public static float Murmur(int input, int offset)
    {
        uint hash = (uint)(input ^ (offset * 0x5bd1e995));
        hash = (hash ^ (hash >> 15)) * 0x5bd1e995;
        return (hash & 0x7FFFFFFF) / (float)int.MaxValue;
    }

    //Loads the current boundary texture to a boundary lookup array
    private void SetBoundary(){
        int width = boundaryTexture.width;
        int height = boundaryTexture.height;

        boundaryLookup = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                boundaryLookup[x, y] = boundaryTexture.GetPixel(x, y).r > 0.5f;
            }
        }

        //Used for more efficient lookup rather than sampling texture every check interval
    }

    //Changes the star's seed to a new seed. Essentially changes the star
    public IEnumerator ReloadStar(int seed)
    {   
        //Stop particle emission to let the current star fade out
        particleSystem.Stop();
        yield return new WaitForSeconds(10f);
        
        //Set the colour of the new star
        float hue = Murmur(seed, 0);
        ChangeColour(hue);

        //Set the new boundary
        SetBoundary();

        //Start emission
        particleSystem.Play();
    }
}
