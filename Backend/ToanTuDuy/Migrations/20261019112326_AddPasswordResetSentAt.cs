using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCOUNT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    refresh_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    password_reset_sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCOUNT", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BADGE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    icon_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    condition_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    condition_value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BADGE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "COURSE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    min_age = table.Column<int>(type: "int", nullable: false),
                    max_age = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COURSE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_ACCOUNT_UserId",
                        column: x => x.UserId,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_ACCOUNT_UserId",
                        column: x => x.UserId,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_ACCOUNT_UserId",
                        column: x => x.UserId,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EMAIL_VERIFICATION_TOKEN",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    email_at_issue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAIL_VERIFICATION_TOKEN", x => x.id);
                    table.ForeignKey(
                        name: "FK_EMAIL_VERIFICATION_TOKEN_ACCOUNT_account_id",
                        column: x => x.account_id,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PARENT_PROFILE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pin = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARENT_PROFILE", x => x.id);
                    table.ForeignKey(
                        name: "FK_PARENT_PROFILE_ACCOUNT_account_id",
                        column: x => x.account_id,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TEACHER_PROFILE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    specialize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TEACHER_PROFILE", x => x.id);
                    table.ForeignKey(
                        name: "FK_TEACHER_PROFILE_ACCOUNT_account_id",
                        column: x => x.account_id,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_ACCOUNT_UserId",
                        column: x => x.UserId,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "COURSE_LEVEL",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    total_sessions = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COURSE_LEVEL", x => x.id);
                    table.ForeignKey(
                        name: "FK_COURSE_LEVEL_COURSE_course_id",
                        column: x => x.course_id,
                        principalTable: "COURSE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QUESTION_BANK",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QUESTION_BANK", x => x.id);
                    table.ForeignKey(
                        name: "FK_QUESTION_BANK_COURSE_course_id",
                        column: x => x.course_id,
                        principalTable: "COURSE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ref_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION", x => x.id);
                    table.ForeignKey(
                        name: "FK_NOTIFICATION_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_SETTING",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_SETTING", x => x.id);
                    table.ForeignKey(
                        name: "FK_NOTIFICATION_SETTING_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STUDENT_PROFILE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    total_points = table.Column<int>(type: "int", nullable: false),
                    gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stars = table.Column<int>(type: "int", nullable: false),
                    streak_days = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STUDENT_PROFILE", x => x.id);
                    table.ForeignKey(
                        name: "FK_STUDENT_PROFILE_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CLASSES",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_level_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    max_capacity = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLASSES", x => x.id);
                    table.ForeignKey(
                        name: "FK_CLASSES_COURSE_LEVEL_course_level_id",
                        column: x => x.course_level_id,
                        principalTable: "COURSE_LEVEL",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CLASSES_TEACHER_PROFILE_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "TEACHER_PROFILE",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "QUESTION",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bank_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QUESTION", x => x.id);
                    table.ForeignKey(
                        name: "FK_QUESTION_QUESTION_BANK_bank_id",
                        column: x => x.bank_id,
                        principalTable: "QUESTION_BANK",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentBadge",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    badge_id = table.Column<int>(type: "int", nullable: false),
                    earned_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBadge", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentBadge_BADGE_badge_id",
                        column: x => x.badge_id,
                        principalTable: "BADGE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentBadge_STUDENT_PROFILE_student_id",
                        column: x => x.student_id,
                        principalTable: "STUDENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ASSIGNMENT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    stage_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    time_limit_minutes = table.Column<int>(type: "int", nullable: false),
                    max_attempt = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASSIGNMENT", x => x.id);
                    table.ForeignKey(
                        name: "FK_ASSIGNMENT_CLASSES_class_id",
                        column: x => x.class_id,
                        principalTable: "CLASSES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CLASS_SCHEDULE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    day_of_week = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    room = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLASS_SCHEDULE", x => x.id);
                    table.ForeignKey(
                        name: "FK_CLASS_SCHEDULE_CLASSES_class_id",
                        column: x => x.class_id,
                        principalTable: "CLASSES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CLASS_STUDENT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    joined_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLASS_STUDENT", x => x.id);
                    table.ForeignKey(
                        name: "FK_CLASS_STUDENT_CLASSES_class_id",
                        column: x => x.class_id,
                        principalTable: "CLASSES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CLASS_STUDENT_STUDENT_PROFILE_student_id",
                        column: x => x.student_id,
                        principalTable: "STUDENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "INVOICE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    transaction_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICE", x => x.id);
                    table.ForeignKey(
                        name: "FK_INVOICE_CLASSES_class_id",
                        column: x => x.class_id,
                        principalTable: "CLASSES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INVOICE_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INVOICE_STUDENT_PROFILE_student_id",
                        column: x => x.student_id,
                        principalTable: "STUDENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LESSON",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    lesson_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_importan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LESSON", x => x.id);
                    table.ForeignKey(
                        name: "FK_LESSON_CLASSES_class_id",
                        column: x => x.class_id,
                        principalTable: "CLASSES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QUESTION_ASSIGNMENT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignment_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    point_value = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QUESTION_ASSIGNMENT", x => x.id);
                    table.ForeignKey(
                        name: "FK_QUESTION_ASSIGNMENT_ASSIGNMENT_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "ASSIGNMENT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QUESTION_ASSIGNMENT_QUESTION_question_id",
                        column: x => x.question_id,
                        principalTable: "QUESTION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STUDENT_ASSIGNMENT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignment_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<float>(type: "real", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    teacher_feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STUDENT_ASSIGNMENT", x => x.id);
                    table.ForeignKey(
                        name: "FK_STUDENT_ASSIGNMENT_ASSIGNMENT_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "ASSIGNMENT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STUDENT_ASSIGNMENT_STUDENT_PROFILE_student_id",
                        column: x => x.student_id,
                        principalTable: "STUDENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ATTENDANCE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    lesson_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    teacher_notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ATTENDANCE", x => x.id);
                    table.ForeignKey(
                        name: "FK_ATTENDANCE_LESSON_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "LESSON",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ATTENDANCE_STUDENT_PROFILE_student_id",
                        column: x => x.student_id,
                        principalTable: "STUDENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LEAVE_REQUEST",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    lesson_id = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEAVE_REQUEST", x => x.id);
                    table.ForeignKey(
                        name: "FK_LEAVE_REQUEST_LESSON_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "LESSON",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LEAVE_REQUEST_STUDENT_PROFILE_student_id",
                        column: x => x.student_id,
                        principalTable: "STUDENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "ACCOUNT",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "ACCOUNT",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ASSIGNMENT_class_id",
                table: "ASSIGNMENT",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_ATTENDANCE_lesson_id",
                table: "ATTENDANCE",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_ATTENDANCE_student_id",
                table: "ATTENDANCE",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_SCHEDULE_class_id",
                table: "CLASS_SCHEDULE",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_STUDENT_class_id",
                table: "CLASS_STUDENT",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_STUDENT_student_id",
                table: "CLASS_STUDENT",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_CLASSES_course_level_id",
                table: "CLASSES",
                column: "course_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_CLASSES_teacher_id",
                table: "CLASSES",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_COURSE_LEVEL_course_id",
                table: "COURSE_LEVEL",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMAIL_VERIFICATION_TOKEN_account_id",
                table: "EMAIL_VERIFICATION_TOKEN",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMAIL_VERIFICATION_TOKEN_token_hash",
                table: "EMAIL_VERIFICATION_TOKEN",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_class_id",
                table: "INVOICE",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_parent_id",
                table: "INVOICE",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_student_id",
                table: "INVOICE",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_LEAVE_REQUEST_lesson_id",
                table: "LEAVE_REQUEST",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_LEAVE_REQUEST_student_id",
                table: "LEAVE_REQUEST",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_LESSON_class_id",
                table: "LESSON",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_parent_id",
                table: "NOTIFICATION",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_parent_id",
                table: "NOTIFICATION_SETTING",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_PARENT_PROFILE_account_id",
                table: "PARENT_PROFILE",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_bank_id",
                table: "QUESTION",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_ASSIGNMENT_assignment_id",
                table: "QUESTION_ASSIGNMENT",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_ASSIGNMENT_question_id",
                table: "QUESTION_ASSIGNMENT",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_BANK_course_id",
                table: "QUESTION_BANK",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_ASSIGNMENT_assignment_id",
                table: "STUDENT_ASSIGNMENT",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_ASSIGNMENT_student_id",
                table: "STUDENT_ASSIGNMENT",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENT_PROFILE_parent_id",
                table: "STUDENT_PROFILE",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadge_badge_id",
                table: "StudentBadge",
                column: "badge_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadge_student_id",
                table: "StudentBadge",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_TEACHER_PROFILE_account_id",
                table: "TEACHER_PROFILE",
                column: "account_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ATTENDANCE");

            migrationBuilder.DropTable(
                name: "CLASS_SCHEDULE");

            migrationBuilder.DropTable(
                name: "CLASS_STUDENT");

            migrationBuilder.DropTable(
                name: "EMAIL_VERIFICATION_TOKEN");

            migrationBuilder.DropTable(
                name: "INVOICE");

            migrationBuilder.DropTable(
                name: "LEAVE_REQUEST");

            migrationBuilder.DropTable(
                name: "NOTIFICATION");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_SETTING");

            migrationBuilder.DropTable(
                name: "QUESTION_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "STUDENT_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "StudentBadge");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "LESSON");

            migrationBuilder.DropTable(
                name: "QUESTION");

            migrationBuilder.DropTable(
                name: "ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "BADGE");

            migrationBuilder.DropTable(
                name: "STUDENT_PROFILE");

            migrationBuilder.DropTable(
                name: "QUESTION_BANK");

            migrationBuilder.DropTable(
                name: "CLASSES");

            migrationBuilder.DropTable(
                name: "PARENT_PROFILE");

            migrationBuilder.DropTable(
                name: "COURSE_LEVEL");

            migrationBuilder.DropTable(
                name: "TEACHER_PROFILE");

            migrationBuilder.DropTable(
                name: "COURSE");

            migrationBuilder.DropTable(
                name: "ACCOUNT");
        }
    }
}
