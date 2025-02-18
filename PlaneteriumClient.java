import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;

public class PlaneteriumClient {
    public static void main(String[] args) {
        // Ensure there are two arguments: systemId and seed
        if (args.length != 2) {
            System.out.println("Usage: java PlaneteriumClient <systemId> <seed>");
            return;
        }

        try {
            // Parse the systemId and seed
            int systemId = Integer.parseInt(args[0]);
            int seed = Integer.parseInt(args[1]);

            // Create an HttpClient
            HttpClient client = HttpClient.newHttpClient();

            // Construct the JSON payload for the POST request
            String jsonInputString = "{\n" +
                    "  \"systemId\": " + systemId + ",\n" +
                    "  \"seed\": " + seed + "\n" +
                    "}";

            // Create a POST request with the JSON body
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(new URI("http://127.0.0.1:8080/"))
                    .header("Content-Type", "application/json")
                    .POST(HttpRequest.BodyPublishers.ofString(jsonInputString))
                    .build();

            // Send the request and receive the response
            HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());

            // Print the response from the server
            System.out.println("Response Code: " + response.statusCode());
            System.out.println("Response Body: " + response.body());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}