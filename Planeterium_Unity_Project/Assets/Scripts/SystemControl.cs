using UnityEngine;
using System.Net;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using System;

/*
This script handles requests from external processes to update a given star
For the current version, there are 3 defined star positions. This script accepts a json object that
defines the star index and the new seed, and updates the star accordingly 
*/
public class SystemControl : MonoBehaviour
{
    [Header("Seeds")]
    [SerializeField] private int[] seeds = new int[3];  //Array for seeds of the current stars

    [Header("Particle Systems")]
    [SerializeField] private ParticleControl[] particleSystems = new ParticleControl[3];  //Array for particle systems

    [SerializeField] private BlobGenerator blobGenerator;   //Blob generator

    private Texture2D[] blobTextures = new Texture2D[3];

    private HttpListener httpListener;
    private SynchronizationContext mainThreadContext;

    void Start()
    {
        // Store the main thread synchronization context
        mainThreadContext = SynchronizationContext.Current;

        //Initialize stars
        for (int i = 0; i < seeds.Length; i++)
        {
            blobTextures[i] = blobGenerator.GenerateBlob(seeds[i]); //Get boundary texture for the star
            particleSystems[i].boundaryTexture = blobTextures[i];  //Apply the boundary texture
            StartCoroutine(particleSystems[i].ReloadStar(1));  //Initialize star
        }

        //Start the web server in a separate thread to avoid blocking the main Unity thread
        Thread serverThread = new Thread(StartWebServer);
        serverThread.Start();
    }

    //Method for redrawing a star based on the requested star id and new seed
    private void UpdateStar(int systemId, int seed)
    {
        //Post the GenerateBlob task to the main thread using SynchronizationContext
        mainThreadContext.Post(_ =>
        {
            blobTextures[systemId] = blobGenerator.GenerateBlob(seed);

            //Assign the generated texture to the appropriate particle system
            particleSystems[systemId].boundaryTexture = blobTextures[systemId];

            //Update the star
            StartCoroutine(particleSystems[systemId].ReloadStar(seed));
        }, null);
    }

    //Starts an server to listen for external requests to update a star
    private void StartWebServer()
    {
        httpListener = new HttpListener();

        //Listen strictly on http://localhost:8080/
        httpListener.Prefixes.Add("http://127.0.0.1:8080/");

        httpListener.Start();
        Debug.Log("Web Server Started on http://localhost:8080/...");

        while (true)
        {
            HttpListenerContext context = httpListener.GetContext();
            ThreadPool.QueueUserWorkItem(HandleRequest, context);
        }
    }

    //Method to handle star update requests. The request is a json object with 2 ints, the first defines the index
    //of the star to update, and the second is the new star seed
    private void HandleRequest(object obj)
    {
        HttpListenerContext context = (HttpListenerContext)obj;
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        //Log that the request is received and details of the request
        Debug.Log("Received request from: " + request.RemoteEndPoint.ToString());
        Debug.Log("Request Method: " + request.HttpMethod);
        Debug.Log("Request URL: " + request.Url.ToString());
        Debug.Log("Request Headers: " + request.Headers.ToString());

        //Parse the incoming request body and log
        string requestBody = new System.IO.StreamReader(request.InputStream).ReadToEnd();
        Debug.Log("Request Body: " + requestBody);

        //Deserialize the request data and log
        RequestData requestData = JsonConvert.DeserializeObject<RequestData>(requestBody);
        Debug.Log($"Parsed request data - systemId: {requestData.systemId}, seed: {requestData.seed}");

        // Handle the request
        if (requestData.systemId >= 0 && requestData.systemId < particleSystems.Length)
        {
            Debug.Log("Request is valid");
            int requested_id = requestData.systemId;
            int requested_seed = requestData.seed;

            UpdateStar(requested_id, requested_seed);

            // Log that the texture has been successfully generated and assigned
            Debug.Log($"Blob generated and assigned to system {requestData.systemId}.");
        }
        else
        {
            Debug.Log("Invalid: " + requestData.systemId);
        }

        // Respond with a success message
        string responseMessage = $"Blob for system {requestData.systemId} generated successfully.";
        byte[] buffer = Encoding.UTF8.GetBytes(responseMessage);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private class RequestData
    {
        public int systemId;
        public int seed;
    }

    private void OnApplicationQuit()
    {
        httpListener.Stop();
    }
}
