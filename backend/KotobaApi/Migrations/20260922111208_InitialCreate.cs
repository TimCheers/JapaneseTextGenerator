using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KotobaApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    native_language = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "decks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decks", x => x.id);
                    table.ForeignKey(
                        name: "fk_decks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generation_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    prompt_params = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generation_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_generation_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "words",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deck_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term = table.Column<string>(type: "text", nullable: false),
                    reading = table.Column<string>(type: "text", nullable: true),
                    meaning = table.Column<string>(type: "text", nullable: false),
                    part_of_speech = table.Column<string>(type: "text", nullable: true),
                    jlpt_level = table.Column<int>(type: "integer", nullable: true),
                    example_sentence = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    acquisition_source = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_words", x => x.id);
                    table.ForeignKey(
                        name: "fk_words_decks_deck_id",
                        column: x => x.deck_id,
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generated_texts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generation_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generated_texts", x => x.id);
                    table.ForeignKey(
                        name: "fk_generated_texts_generation_requests_generation_request_id",
                        column: x => x.generation_request_id,
                        principalTable: "generation_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generation_request_words",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generation_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generation_request_words", x => x.id);
                    table.ForeignKey(
                        name: "fk_generation_request_words_generation_requests_generation_req",
                        column: x => x.generation_request_id,
                        principalTable: "generation_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_generation_request_words_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "word_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stability = table.Column<double>(type: "double precision", nullable: false),
                    stability_fast = table.Column<double>(type: "double precision", nullable: false),
                    difficulty = table.Column<double>(type: "double precision", nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    lapse_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_word_progress_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_word_progress_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "word_review_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    elapsed_seconds = table.Column<int>(type: "integer", nullable: false),
                    desired_retention = table.Column<double>(type: "double precision", nullable: false),
                    scheduler_version = table.Column<string>(type: "text", nullable: false),
                    stability_before = table.Column<double>(type: "double precision", nullable: false),
                    stability_after = table.Column<double>(type: "double precision", nullable: false),
                    stability_fast_before = table.Column<double>(type: "double precision", nullable: false),
                    stability_fast_after = table.Column<double>(type: "double precision", nullable: false),
                    difficulty_before = table.Column<double>(type: "double precision", nullable: false),
                    difficulty_after = table.Column<double>(type: "double precision", nullable: false),
                    due_at_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_review_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_word_review_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_word_review_events_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comprehension_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_text_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: false),
                    correct_answer = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comprehension_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_comprehension_questions_generated_texts_generated_text_id",
                        column: x => x.generated_text_id,
                        principalTable: "generated_texts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "generated_text_word_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_text_id = table.Column<Guid>(type: "uuid", nullable: false),
                    word_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrences = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generated_text_word_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_generated_text_word_usages_generated_texts_generated_text_id",
                        column: x => x.generated_text_id,
                        principalTable: "generated_texts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_generated_text_word_usages_words_word_id",
                        column: x => x.word_id,
                        principalTable: "words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "practice_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_text_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_practice_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_practice_attempts_generated_texts_generated_text_id",
                        column: x => x.generated_text_id,
                        principalTable: "generated_texts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_practice_attempts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    practice_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comprehension_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_answer = table.Column<string>(type: "text", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_answers", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_answers_comprehension_questions_comprehension_question",
                        column: x => x.comprehension_question_id,
                        principalTable: "comprehension_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_answers_practice_attempts_practice_attempt_id",
                        column: x => x.practice_attempt_id,
                        principalTable: "practice_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comprehension_questions_generated_text_id",
                table: "comprehension_questions",
                column: "generated_text_id");

            migrationBuilder.CreateIndex(
                name: "ix_decks_user_id",
                table: "decks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_generated_text_word_usages_generated_text_id",
                table: "generated_text_word_usages",
                column: "generated_text_id");

            migrationBuilder.CreateIndex(
                name: "ix_generated_text_word_usages_word_id",
                table: "generated_text_word_usages",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_generated_texts_generation_request_id",
                table: "generated_texts",
                column: "generation_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_words_generation_request_id",
                table: "generation_request_words",
                column: "generation_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_generation_request_words_word_id",
                table: "generation_request_words",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_generation_requests_user_id",
                table: "generation_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_practice_attempts_generated_text_id",
                table: "practice_attempts",
                column: "generated_text_id");

            migrationBuilder.CreateIndex(
                name: "ix_practice_attempts_user_id",
                table: "practice_attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_answers_comprehension_question_id",
                table: "user_answers",
                column: "comprehension_question_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_answers_practice_attempt_id",
                table: "user_answers",
                column: "practice_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_word_progress_user_id_word_id",
                table: "word_progress",
                columns: new[] { "user_id", "word_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_word_progress_word_id",
                table: "word_progress",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_word_review_events_event_id",
                table: "word_review_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_word_review_events_user_id",
                table: "word_review_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_word_review_events_word_id",
                table: "word_review_events",
                column: "word_id");

            migrationBuilder.CreateIndex(
                name: "ix_words_deck_id",
                table: "words",
                column: "deck_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_text_word_usages");

            migrationBuilder.DropTable(
                name: "generation_request_words");

            migrationBuilder.DropTable(
                name: "user_answers");

            migrationBuilder.DropTable(
                name: "word_progress");

            migrationBuilder.DropTable(
                name: "word_review_events");

            migrationBuilder.DropTable(
                name: "comprehension_questions");

            migrationBuilder.DropTable(
                name: "practice_attempts");

            migrationBuilder.DropTable(
                name: "words");

            migrationBuilder.DropTable(
                name: "generated_texts");

            migrationBuilder.DropTable(
                name: "decks");

            migrationBuilder.DropTable(
                name: "generation_requests");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
