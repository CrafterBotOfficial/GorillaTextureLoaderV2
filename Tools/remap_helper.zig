const std = @import("std");
const Sha256 = std.crypto.hash.sha2.Sha256;

const KnownTextureHash = struct {
    texture_name: []u8,
    hash: []u8,
};

const FileHashResult = struct {
    file_name: []const u8,
    hash: []const u8, // stringy
};

pub fn main(init: std.process.Init) !void {
    const dir_path = try init.minimal.args.toSlice(init.gpa);
    defer init.gpa.free(dir_path);
    var dir = try std.Io.Dir.openDirAbsolute(init.io, dir_path[1], .{ .access_sub_paths = true, .iterate = true });
    defer dir.close(init.io);

    const known_texture_hashes = getTextures(init.io, init.gpa) catch |err| {
        std.log.err("Failed to get known slice hashes {any}", .{err});
        return;
    };
    defer known_texture_hashes.deinit();


    var futures = std.ArrayList(std.Io.Future(anyerror!FileHashResult)).empty;
    defer {
        for (futures.items) |*item| {
            const result = item.cancel(init.io) catch continue;
            init.gpa.free(result.file_name);
            init.gpa.free(result.hash);
        }
        futures.deinit(init.gpa);
    }

    var walker = try dir.walk(init.gpa);
    defer walker.deinit();
    while (try walker.next(init.io)) |entry| {
        if (entry.kind != .file) continue;
        const future = try init.io.concurrent(handleFile, .{try std.fmt.allocPrint(init.gpa, "{s}", .{entry.path}), dir, init.io, init.gpa});
        try futures.append(init.gpa, future);
    }

    const width = 25;
    for (futures.items) |*future| {
        const item = try future.await(init.io);
        for (known_texture_hashes.value) |prev| {
            if (std.mem.eql(u8, prev.hash, item.hash)) {
                const space = try init.gpa.alloc(u8, width - prev.texture_name.len);
                defer init.gpa.free(space);
                for (space) |*byte| {
                    byte.* = ' ';
                }

                const file_name = std.fs.path.stem(item.file_name);
                std.log.info("{s} {s} {s}", .{ prev.texture_name, space, file_name});
                break;
            }
        }
    }

    std.log.info("All tasks complete!", .{});
}

fn handleFile(path: []u8, dir: std.Io.Dir, io: std.Io, gpa: std.mem.Allocator) anyerror!FileHashResult {
    var file = try dir.openFile(io, path, .{ .mode = .read_write });
    defer file.close(io);

    var file_map = try file.createMemoryMap(io, .{ .len = try file.length(io), });
    defer file_map.destroy(io);

    try file_map.read(io);

    var hash_buf: [Sha256.digest_length]u8 = undefined;
    Sha256.hash(file_map.memory, &hash_buf, .{});
    const str = std.fmt.bytesToHex(hash_buf, .lower);

    return .{
        .file_name = path,
        .hash = try std.fmt.allocPrint(gpa, "{s}", .{ str }),
    };
}

fn getTextures(io: std.Io, gpa: std.mem.Allocator) !std.json.Parsed([]KnownTextureHash) {
    const json_path = "named_texture_hashes.json";
    const file_slice = try std.Io.Dir.cwd().readFileAlloc(io, json_path, gpa, .unlimited);
    defer gpa.free(file_slice);

    return try std.json.parseFromSlice([]KnownTextureHash, gpa, file_slice, .{});
}
