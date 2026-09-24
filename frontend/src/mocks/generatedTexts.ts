import type { GeneratedText } from "../types/generatedText";

export const mockGeneratedTexts: GeneratedText[] = [
  {
    id: "1",
    generationRequestId: "mock-1",
    content:
      "今日は天気がとても良いので、公園を散歩することにしました。桜の花が満開で、たくさんの人がお花見を楽しんでいました。",
    createdAt: new Date().toISOString(),
  },
  {
    id: "2",
    generationRequestId: "mock-2",
    content:
      "友達と一緒に新しいレストランへ行きました。メニューには珍しい料理がたくさんあって、何を注文するか迷ってしまいました。",
    createdAt: new Date().toISOString(),
  },
  {
    id: "3",
    generationRequestId: "mock-3",
    content:
      "来週の日本語能力試験に向けて、毎日漢字を勉強しています。難しい単語も多いですが、少しずつ覚えられるようになってきました。",
    createdAt: new Date().toISOString(),
  },
];