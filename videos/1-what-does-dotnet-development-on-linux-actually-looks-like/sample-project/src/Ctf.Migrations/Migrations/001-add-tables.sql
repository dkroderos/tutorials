CREATE TABLE IF NOT EXISTS users(
    id uuid PRIMARY KEY,
    username varchar(64) NOT NULL UNIQUE,
    email varchar(256) NOT NULL UNIQUE,
    password_hash varchar(256),
    is_verified boolean NOT NULL DEFAULT FALSE,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    registration_ip inet NOT NULL
);

CREATE TABLE user_providers(
    provider varchar(50) NOT NULL,
    provider_id varchar(200) NOT NULL,
    user_id uuid UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    PRIMARY KEY (provider, provider_id)
);

CREATE TABLE IF NOT EXISTS rooms(
    id uuid PRIMARY KEY,
    creator_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name varchar(64) NOT NULL,
    description varchar(200),
    are_challenges_hidden boolean NOT NULL DEFAULT FALSE,
    is_submissions_force_disabled boolean NOT NULL DEFAULT FALSE,
    allow_player_created_teams boolean NOT NULL DEFAULT FALSE,
    allow_players_to_view_other_team_solves boolean NOT NULL DEFAULT FALSE,
    submission_start timestamptz,
    submission_end timestamptz,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    UNIQUE (creator_id, name)
);

CREATE TYPE room_role AS ENUM(
    'owner',
    'admin',
    'editor',
    'player'
);

CREATE TABLE IF NOT EXISTS room_invitations(
    room_id uuid NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    invitee_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    inviter_id uuid NOT NULL REFERENCES users(id) ON DELETE SET NULL,
    room_role room_role NOT NULL,
    invited_at timestamptz NOT NULL DEFAULT NOW(),
    accepted_at timestamptz,
    PRIMARY KEY (room_id, invitee_id)
);

CREATE TABLE IF NOT EXISTS room_members(
    room_id uuid NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at timestamptz NOT NULL DEFAULT NOW(),
    room_role room_role NOT NULL,
    PRIMARY KEY (room_id, user_id)
);

CREATE TABLE IF NOT EXISTS teams(
    id uuid PRIMARY KEY,
    room_id uuid NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    name varchar(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    UNIQUE (room_id, name)
);

CREATE TABLE IF NOT EXISTS team_members(
    team_id uuid NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    user_id uuid NOT NULL,
    room_id uuid NOT NULL,
    joined_at timestamptz NOT NULL DEFAULT NOW(),
    PRIMARY KEY (team_id, user_id),
    FOREIGN KEY (room_id, user_id) REFERENCES room_members(room_id, user_id) ON DELETE CASCADE,
    FOREIGN KEY (team_id) REFERENCES teams(id) ON DELETE CASCADE,
    UNIQUE (room_id, user_id)
);

CREATE TABLE IF NOT EXISTS challenges(
    id uuid PRIMARY KEY,
    room_id uuid NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    creator_id uuid REFERENCES users(id) ON DELETE SET NULL,
    updater_id uuid REFERENCES users(id) ON DELETE SET NULL,
    name varchar(64) NOT NULL,
    description varchar(2000) NOT NULL,
    max_attempts integer NOT NULL CHECK (max_attempts >= 0),
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_at timestamptz,
    UNIQUE (room_id, name)
);

CREATE TABLE IF NOT EXISTS artifacts(
    challenge_id uuid NOT NULL REFERENCES challenges(id) ON DELETE CASCADE,
    file_name varchar(255) NOT NULL,
    file_size bigint NOT NULL,
    content_type varchar(255) NOT NULL,
    uploader_id uuid REFERENCES users(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    PRIMARY KEY (challenge_id, file_name)
);

CREATE TABLE IF NOT EXISTS flags(
    challenge_id uuid NOT NULL REFERENCES challenges(id) ON DELETE CASCADE,
    value varchar(500) NOT NULL,
    PRIMARY KEY (challenge_id, value)
);

CREATE TABLE IF NOT EXISTS tags(
    challenge_id uuid NOT NULL REFERENCES challenges(id) ON DELETE CASCADE,
    value varchar(20) NOT NULL,
    PRIMARY KEY (challenge_id, value)
);

CREATE TABLE IF NOT EXISTS solves(
    challenge_id uuid NOT NULL REFERENCES challenges(id) ON DELETE CASCADE,
    team_id uuid NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    solved_at timestamptz NOT NULL DEFAULT NOW()
);

