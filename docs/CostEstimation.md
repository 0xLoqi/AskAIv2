Cost Estimation: API Usage vs Local Compute
Understanding cost implications is crucial for choosing what runs locally vs in the cloud. Below we estimate the monthly API costs per user for various usage levels, and how local processing can mitigate some costs: Assumptions for cost calculations:
OpenAI GPT-4 API pricing (8k context): $0.03 per 1K prompt tokens, $0.06 per 1K response tokens​
help.openai.com
. In practice, 1 query (with history) might total ~1000 tokens (prompt + answer), costing ~$0.045. We’ll assume ~$0.05 per text query on average for simplicity (some shorter, some longer).
GPT-4 Vision: Currently priced similarly per token (no extra image charge explicitly, beyond tokens needed to describe the image internally). Vision might produce longer answers or need detailed prompts, but we’ll fold that into the per-query token count.
Whisper API: $0.006 per minute of audio​
openai.com
. Even a heavy voice user might only use a few minutes per day for queries.
OmniParser: Runs locally (open source), so no direct cost – just uses the user’s CPU/GPU for a few seconds when needed.
Selenium: No cost (local).
OCR: Free (local).
We exclude costs of the backend (Auth and database) as they are relatively small per user (Firebase’s free tier covers a lot; beyond that, maybe cents per 1000 users for data reads/writes).
Now, approximate monthly usage scenarios:

Usage Tier	Example Usage Per Month (per user)	Est. Tokens (for GPT)	Est. API Cost (OpenAI)
Light	~50 queries (text-only), 5 image-based questions, 10 minutes of voice total.	~50k tokens (0.05M)	GPT-4: ~$2.50 – $3.00
Whisper: ~$0.06
Medium	~200 queries, 20 image-based, 1 hour voice total (60 min).	~200k tokens (0.2M)	GPT-4: ~$10 – $12
Whisper: ~$0.36
Heavy	~1000 queries, 100 image-based, 5 hours voice (300 min).	~1,000k tokens (1M)	GPT-4: ~$50 – $60
Whisper: ~$1.80
Very Heavy	(Power user) 2,000+ queries, many with images. Possibly 2M+ tokens.	2,000k tokens (2M)	GPT-4: ~$120+ (scales linearly)
Whisper: ~$3.60
(Cost notes: The token estimates assume ~1000 tokens per query on average for GPT-4. Many queries will be smaller, but some complex ones or with large context could be more. Whisper cost is minimal even for heavy voice users: 5 hours of speech is only $1.80.) From the above:
A “typical” active user (Medium tier) might incur around $10–15 of OpenAI costs monthly if using GPT-4 for everything. Lighter or infrequent users will be only a few dollars. The heavy power user could cost $50+ if unrestricted.
Using GPT-3.5 for some fraction of queries can cut costs dramatically (it’s ~1/15th the cost of GPT-4 per token). For instance, if half the queries (especially easy ones) went to GPT-3.5, the Medium user’s cost might drop to ~$6 instead of $12. We’d balance this against answer quality.
Local processing savings: If the user or design chooses local Whisper, the voice cost becomes $0 (just CPU usage). Local OCR and even some image parsing via OmniParser means we might avoid calling GPT-4 for certain quick tasks (like if the user just wants the text from an image, we can do that fully locally). These save a bit of cost and also work offline.
There is also the possibility of using GPT-4 Turbo (Vision) model, which OpenAI introduced with lower prices. For example, GPT-4 Turbo might cost $0.01/1K input and $0.03/1K output​
help.openai.com
, roughly one-third the cost of original GPT-4. If we use Turbo when available, the above GPT-4 costs could be 1/2 to 1/3 cheaper, making heavy usage more affordable. We’ll assume our API usage will evolve to the most cost-effective GPT-4 version available.
Infrastructure costs (for our service): Minimal, since computation is mostly on OpenAI’s side (paid per use) or the client’s side. The backend services (Firebase etc.) might cost a few dollars per 1000 users (for database storage and bandwidth) – which is negligible compared to the AI API costs.