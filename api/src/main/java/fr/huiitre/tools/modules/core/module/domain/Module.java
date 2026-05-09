package fr.huiitre.tools.modules.core.module.domain;

import java.time.LocalDateTime;

public class Module {
	private Long id;
	private String code;
	private String name;
	private String description;
	private Boolean active;
	private LocalDateTime createdAt;
	private LocalDateTime updatedAt;

	public Module(
			String code,
			String name,
			String description) {
		this.code = code;
		this.name = name;
		this.description = description;
	}

	public static Module create(
			String code,
			String name,
			String description) {
		if (code == null || code.isBlank())
			throw new IllegalArgumentException("CODE_REQUIRED");

		if (name == null || name.isBlank())
			throw new IllegalArgumentException("NAME_REQUIRED");

		if (description == null || description.isBlank())
			throw new IllegalArgumentException("DESCRIPTION_REQUIRED");

		Module module = new Module(code, name, description);

		module.setActive(false);

		return module;
	}

	public void update(
			String code,
			String name,
			String description,
			Boolean active) {
		if (code != null) {
			this.code = validateCode(code);
		}

		if (name != null) {
			this.name = validateName(name);
		}

		if (description != null) {
			this.description = description;
		}

		if (active != null) {
			this.active = active;
		}
	}

	private static String validateCode(String code) {
		if (code.isBlank()) {
			throw new IllegalArgumentException("CODE_INVALID");
		}
		return code;
	}

	private static String validateName(String name) {
		if (name.isBlank()) {
			throw new IllegalArgumentException("NAME_INVALID");
		}
		return name;
	}

	public Long getId() {
		return this.id;
	}

	public Long setId(Long id) {
		return this.id = id;
	}

	public String getCode() {
		return this.code;
	}

	public String getName() {
		return this.name;
	}

	public String getDescription() {
		return this.description;
	}

	public boolean getActive() {
		return this.active;
	}

	public void setActive(boolean value) {
		this.active = value;
	}

	public LocalDateTime getCreatedAt() {
		return this.createdAt;
	}

	public void setCreatedAt(LocalDateTime createdAt) {
		this.createdAt = createdAt;
	}

	public LocalDateTime getUpdatedAt() {
		return this.updatedAt;
	}

	public void setUpdatedAt(LocalDateTime value) {
		this.updatedAt = value;
	}
}
