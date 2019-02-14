

//var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

//// Disable send button until connection is established
//document.getElementById("sendButton").disabled = true;

//connection.on("ReceiveMessage", function (user, message) {
//    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
//    var encodedMsg = user + " says " + msg;
//    var li = document.createElement("li");
//    li.textContent = encodedMsg;
//    document.getElementById("messagesList").appendChild(li);
//});



//document.getElementById("sendButton").addEventListener("click", function (event) {
//    var user = document.getElementById("userInput").value;
//    var message = document.getElementById("messageInput").value;
//    connection.invoke("SendMessage", user, message).catch(function (err) {
//        return console.error(err.toString());
//    });
//    event.preventDefault();
//});

$(function () {

    var ready;
    var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

    connection.start()
        .then(function () {
            $("#join").disabled = false;

        })
        .catch(function (err) {
            return console.error(err.toString());
        });

    $("#chat").hide();
    $("#lobby").hide();
    $("#name").focus();
    $("form").submit(function (event) {
        event.preventDefault();
    });

    $("#join").click(function () {
        var name = $("#name").val();
        if (name != "") {
            connection.invoke("Join", name);
            $("#login").detach();
            $("#lobby").show();
            //$("#chat").show();
            //$("#msg").focus()
            //ready = true;
        }
    });

    $("#name").keypress(function (e) {
        if (e.which == 13) {
            var name = $("#name").val();
            if (name != "") {
                connection.invoke("Join", name);
                $("#login").detach();
                $("#lobby").show();
                //$("#chat").show();
                //$("#msg").focus()
                //ready = true;
            }
        }
    });

    $("#createLobby").click(function () {
        var lobbyName = $("#lobbyName").val();
        if (lobbyName != "") {
            connection.invoke("CreateRoom", lobbyName);
            $("#lobby").detach();
            $("#chat").show();
            $("#msg").focus()
            ready = true;
        }
    });

    $("#send").click(function () {
        var msg = $("#msg").val();
        connection.invoke("Send", msg);
        $("#msg").val("");
    });

    var timer;
    var interval = 5000;

    $("#msg").keypress(function (e) {
        if (e.which == 13) {
            var msg = $("#msg").val();
            connection.invoke("Send", msg);
            $("#msg").val("");
        } else {
            clearTimeout(timer);
            connection.invoke("PeopleTyping", true);
        }
    });

    $("#msg").keyup(function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            connection.invoke("PeopleTyping", false)
        }, interval);
    });

    connection.on("update-rooms", function (rooms) {
        $("room-list").empty();
        var _rooms = JSON.parse(rooms);

        if (_rooms != "") {
            _rooms.forEach(room => {
                const tr = document.createElement('tr')

                tr.innerHTML = `
                    <th scope="row">${room.RoomName}</th>
                    <td>${room.UsersInRoom.length}</td>
                    <td>
                      <form>
                        <input class="btn btn-primary btn-sm btn-block" type="button" id="join" value="${room.RoomName}" />
                      </form>
                    </td>
                  `

                document.getElementById('room-list').appendChild(tr)
            })
        }
    });

    connection.on("update", function (msg) {
        if (ready) {
            $("#msgs").append("<li>" + msg + "</li>");
        }
    });

    connection.on("update-people", function (people) {
        if (ready) {
            $("#people").empty();
            var _people = JSON.parse(people);
            $.each(_people, function (u, user) {
                var person = _people[u].Name;
                var li = document.createElement("li");
                li.textContent = person;
                document.getElementById("people").appendChild(li);
            });
        }
    });

    connection.on("chat", function (who, msg) {
        if (ready) {
            $("#msgs").append('<li><strong><span class="text-success">' + who + '</span></strong> says: ' + msg + "</li>");
        }
    });

    connection.on('typing', function (who, msg) {
        if (ready) {
            $('#people-typing').html(who + ' ' + msg);
        }
    });

    connection.on('not-typing', function (msg) {
        if (ready) {
            $('#people-typing').html(msg);
        }
    })

    connection.onclose(function () {
        $('#msgs').append('<li><strong><span class="text-warning">The server is not available</span></strong></li>');
        $('#msg').atrr("disabled", "disabled");
        $('#send').atrr('disabled', 'disabled');
    })
});