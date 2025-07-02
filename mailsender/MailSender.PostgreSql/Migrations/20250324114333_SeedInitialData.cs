using Microsoft.EntityFrameworkCore.Migrations;
using System.Linq;

#nullable disable

namespace MailSender.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "statuses",
                columns: new[] { "id", "name", "description" },
                values: new object[,]
                {
                    {
                        1,
                        "Created",
                        "Создано сообщение"
                    },
                    {
                        2,
                        "StartSended",
                        "Происходит отправка сообщения"
                    },
                    {
                        3,
                        "EndSended",
                        "Сообщение успешно доставлено"
                    },
                    {
                        4,
                        "Error",
                        "Возникла ошибка отправки"
                    },
                    {
                        5,
                        "TemplateReceived",
                        "Шаблон получен"
                    },
                    {
                        6,
                        "Retry",
                        "Повторная отправка"
                    },
                    {
                        7,
                        "Duplicated",
                        "Обнаружен дубль сообщения"
                    },
                    {
                        8,
                        "StartSendedViaCommon",
                        "Начинаем отправку через Common"
                    },
                    {
                        9,
                        "EndSendedViaCommon",
                        "Отправка заверешена через Common"
                    },
                    {
                        10,
                        "StartSendedViaReserve",
                        "Начинаем отправку через Reserve"
                    },
                    {
                        11,
                        "EndSendedViaReserve",
                        "Отправка заверешена через Reserve"
                    }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "id",
                keyValues:
                [
                    Enumerable.Range(1, 11)
                ]
            );
        }
    }
}
